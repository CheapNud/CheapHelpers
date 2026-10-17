using CheapHelpers.Blazor.Configuration;
using CheapHelpers.Blazor.Pages.Account;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Minimal API endpoints for the account actions that must write the auth cookie
/// (sign-in, sign-out, refresh). Blazor circuits cannot set cookies, so the account pages
/// post plain forms to these endpoints.
/// </summary>
public static class AccountEndpointExtensions
{
    private const string LoggerCategory = "CheapHelpers.Account";
    private const string ExternalLoginNotRemovedMessage = "The external login was not removed.";

    /// <summary>
    /// Maps the account endpoints under <see cref="AccountRouteOptions.BaseRoute"/> (default <c>/Account</c>):
    /// <c>SignIn</c>, <c>SignInWith2fa</c>, <c>SignInWithRecoveryCode</c>, <c>SignOut</c>, <c>Refresh</c>,
    /// <c>ClearExternalCookie</c>, <c>ResetAuthenticator</c> and <c>RemoveLogin</c>. All of them are POST
    /// and demand an antiforgery token, so <c>app.UseAntiforgery()</c> must run before this call.
    /// </summary>
    /// <param name="endpoints">The app's endpoint route builder.</param>
    /// <param name="configure">Optional hook configuration, see <see cref="AccountEndpointOptions{TUser}"/>.</param>
    public static IEndpointRouteBuilder MapCheapAccountEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        Action<AccountEndpointOptions<TUser>>? configure = null)
        where TUser : CheapUser
    {
        var routes = endpoints.ServiceProvider.GetRequiredService<AccountRouteOptions>();
        var options = new AccountEndpointOptions<TUser>();
        configure?.Invoke(options);

        var group = endpoints.MapGroup("/" + routes.BaseRoute.Trim('/'));

        async Task<IResult> RedirectAfterSignInAsync(TUser user, HttpContext httpContext)
        {
            var target = options.AfterSignIn is null ? null : await options.AfterSignIn(user, httpContext);
            return Results.Redirect(target ?? routes.HomeRoute);
        }

        // Password sign-in. Failures redirect to LoginRoute with ?failed=credentials|notallowed so the
        // login page can show feedback; two-factor continues at TwoFactorRoute, lockouts go to LockoutRoute
        group.MapPost("SignIn", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager, UserManager<TUser> userManager) =>
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // StringValues → string casts yield null for absent fields; guard so a
            // malformed POST can't 500 the endpoint
            var userName = (string?)fc["UserName"] ?? string.Empty;
            var user = await userManager.FindByNameAsync(userName);
            var result = user is null
                ? SignInResult.Failed
                : await signInManager.PasswordSignInAsync(
                    user,
                    (string?)fc["Password"] ?? string.Empty,
                    isPersistent: true,
                    lockoutOnFailure: true);

            LogSignInAttempt(httpContext, userName, result);

            if (result.Succeeded)
            {
                return await RedirectAfterSignInAsync(user!, httpContext);
            }

            return result switch
            {
                { IsLockedOut: true } => Results.Redirect(routes.LockoutRoute),
                { RequiresTwoFactor: true } => Results.Redirect(routes.TwoFactorRoute),
                { IsNotAllowed: true } => Results.Redirect($"{routes.LoginRoute}?failed=notallowed"),
                _ => Results.Redirect($"{routes.LoginRoute}?failed=credentials")
            };
        }).AllowAnonymous();

        // TOTP challenge after a password sign-in that required two-factor. The two-factor user
        // comes from the TwoFactorUserId cookie set by the password sign-in
        group.MapPost("SignInWith2fa", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager) =>
        {
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
            {
                return Results.Redirect(routes.LoginRoute);
            }

            var authenticatorCode = CleanCode((string?)fc["TwoFactorCode"]);
            var rememberMachine = string.Equals(fc["RememberMachine"], "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fc["RememberMachine"], "on", StringComparison.OrdinalIgnoreCase);

            // isPersistent: true matches SignIn, which also always signs in persistent
            // (there is no remember-me choice in this flow); rememberMachine skips the 2FA
            // challenge on this browser next time when the user opted in
            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode,
                isPersistent: true,
                rememberClient: rememberMachine);

            LogSignInAttempt(httpContext, user.UserName, result);

            if (result.Succeeded)
            {
                return await RedirectAfterSignInAsync(user, httpContext);
            }

            return result.IsLockedOut
                ? Results.Redirect(routes.LockoutRoute)
                : Results.Redirect($"{routes.TwoFactorRoute}?failed=code");
        }).AllowAnonymous();

        // Recovery-code sign-in for users without their authenticator. A missing two-factor
        // session redirects to login instead of throwing
        group.MapPost("SignInWithRecoveryCode", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager) =>
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null)
            {
                return Results.Redirect(routes.LoginRoute);
            }

            var recoveryCode = CleanCode((string?)fc["RecoveryCode"]);
            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            LogSignInAttempt(httpContext, user.UserName, result);

            if (result.Succeeded)
            {
                return await RedirectAfterSignInAsync(user, httpContext);
            }

            return result.IsLockedOut
                ? Results.Redirect(routes.LockoutRoute)
                : Results.Redirect($"{routes.RecoveryCodeRoute}?failed=code");
        }).AllowAnonymous();

        group.MapPost("SignOut", async (HttpContext httpContext, SignInManager<TUser> signInManager) =>
        {
            if (signInManager.IsSignedIn(httpContext.User))
            {
                await signInManager.SignOutAsync();
                GetLogger(httpContext).LogInformation("Sign-out - User: {UserName}, IP: {IpAddress}",
                    httpContext.User.Identity?.Name ?? "Unknown", GetIpAddress(httpContext));
            }

            return Results.Redirect(routes.HomeRoute);
        }).RequireAntiforgeryToken();

        group.MapPost("Refresh", async (HttpContext httpContext, SignInManager<TUser> signInManager,
            UserManager<TUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is not null)
            {
                await signInManager.RefreshSignInAsync(user);
            }

            return Results.Redirect(routes.HomeRoute);
        }).RequireAuthorization().RequireAntiforgeryToken();

        // Signs out the temporary external cookie so a fresh external sign-in starts clean.
        // POST, not GET, because it changes state
        group.MapPost("ClearExternalCookie", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return Results.NoContent();
        }).AllowAnonymous().RequireAntiforgeryToken();

        group.MapPost("ResetAuthenticator", async (HttpContext httpContext, SignInManager<TUser> signInManager,
            UserManager<TUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(httpContext.User)}'.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);
            await signInManager.RefreshSignInAsync(user);

            return Results.Redirect(routes.EnableAuthenticatorRoute);
        }).RequireAuthorization().RequireAntiforgeryToken();

        // Full request rather than a circuit call, because removing a login refreshes the cookie
        group.MapPost("RemoveLogin", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager, UserManager<TUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.BadRequest(ExternalLoginNotRemovedMessage);
            }

            var result = await userManager.RemoveLoginAsync(
                user,
                (string?)fc["loginProvider"] ?? string.Empty,
                (string?)fc["providerKey"] ?? string.Empty);
            if (!result.Succeeded)
            {
                return Results.BadRequest(ExternalLoginNotRemovedMessage);
            }

            await signInManager.RefreshSignInAsync(user);
            return Results.Redirect(routes.HomeRoute);
        }).RequireAuthorization();

        return endpoints;
    }

    /// <summary>
    /// Endpoints without a form body get no automatic antiforgery check, and the antiforgery
    /// middleware never short-circuits on its own. The metadata makes the middleware validate,
    /// the filter turns a failed validation into a 400.
    /// </summary>
    private static RouteHandlerBuilder RequireAntiforgeryToken(this RouteHandlerBuilder builder) =>
        builder
            .WithMetadata(new RequireAntiforgeryTokenAttribute())
            .AddEndpointFilter(async (filterContext, next) =>
                filterContext.HttpContext.Features.Get<IAntiforgeryValidationFeature>()?.IsValid == true
                    ? await next(filterContext)
                    : Results.BadRequest());

    private static string CleanCode(string? code) =>
        (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);

    private static ILogger GetLogger(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory);

    private static string GetIpAddress(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

    private static void LogSignInAttempt(HttpContext httpContext, string? userName, SignInResult result) =>
        GetLogger(httpContext).LogInformation(
            "Sign-in attempt - User: {UserName}, Success: {Success}, IP: {IpAddress}, Details: {Description}",
            userName, result.Succeeded, GetIpAddress(httpContext), GetSignInDescription(result));

    private static string GetSignInDescription(SignInResult result) =>
        result switch
        {
            { Succeeded: true } => "Signed in successfully",
            { IsLockedOut: true } => "Account locked out",
            { RequiresTwoFactor: true } => "Requires two-factor authentication",
            { IsNotAllowed: true } => "Sign-in not allowed",
            _ => "Failed sign-in"
        };
}
