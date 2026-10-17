using CheapHelpers.Blazor.Configuration;
using CheapHelpers.Blazor.Helpers;
using CheapHelpers.Blazor.Pages.Account;
using CheapHelpers.Blazor.Services;
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

    // Form field PasskeySubmit.razor.js fills with the browser's credential
    private const string PasskeyCredentialField = "Passkey.CredentialJson";

    // Same cap as the ASP.NET Core template, so one account cannot grow the passkey table without bound
    private const int MaxPasskeys = 100;

    /// <summary>
    /// Maps the account endpoints under <see cref="AccountRouteOptions.BaseRoute"/> (default <c>/Account</c>):
    /// <c>SignIn</c>, <c>SignInWith2fa</c>, <c>SignInWithRecoveryCode</c>, <c>SignOut</c>, <c>Refresh</c>,
    /// <c>ClearExternalCookie</c>, <c>ResetAuthenticator</c>, <c>RemoveLogin</c>, <c>ChangePassword</c>,
    /// <c>SetPassword</c>, <c>PasskeyCreationOptions</c>, <c>PasskeyRequestOptions</c>, <c>SignInWithPasskey</c> and
    /// <c>AddPasskey</c>. All of them are POST
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

        // Password changes rotate the security stamp, so the cookie must be refreshed in a full request
        group.MapPost("ChangePassword", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager, UserManager<TUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Redirect(routes.LoginRoute);
            }

            var newPassword = (string?)fc["NewPassword"] ?? string.Empty;
            var result = ConfirmationMatches(fc, newPassword)
                ? await userManager.ChangePasswordAsync(user, (string?)fc["OldPassword"] ?? string.Empty, newPassword)
                : ConfirmationMismatch;

            return await PasswordResultAsync(result, user, signInManager, routes.ChangePasswordRoute);
        }).RequireAuthorization();

        group.MapPost("SetPassword", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager, UserManager<TUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Redirect(routes.LoginRoute);
            }

            var newPassword = (string?)fc["NewPassword"] ?? string.Empty;
            var result = ConfirmationMatches(fc, newPassword)
                ? await userManager.AddPasswordAsync(user, newPassword)
                : ConfirmationMismatch;

            return await PasswordResultAsync(result, user, signInManager, routes.SetPasswordRoute);
        }).RequireAuthorization();

        // WebAuthn options for registering a passkey, fetched by PasskeySubmit before it asks the authenticator.
        // SignInManager keeps the challenge in a cookie that AddPasskey reads back
        group.MapPost("PasskeyCreationOptions", async (HttpContext httpContext, SignInManager<TUser> signInManager,
            UserManager<TUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.NotFound();
            }

            var userName = await userManager.GetUserNameAsync(user) ?? user.Id;
            var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = user.Id,
                Name = userName,
                DisplayName = userName
            });
            return Results.Content(optionsJson, "application/json");
        }).RequireAuthorization().RequireAntiforgeryToken();

        // WebAuthn options for a passkey sign-in. No user name is taken, so the browser offers every
        // passkey it has for this site and the endpoint reveals nothing about which accounts exist
        group.MapPost("PasskeyRequestOptions", async (SignInManager<TUser> signInManager) =>
        {
            var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(null);
            return Results.Content(optionsJson, "application/json");
        }).AllowAnonymous().RequireAntiforgeryToken();

        // Passkey sign-in posted by PasskeySubmit. Identity skips the two-factor step for passkeys;
        // locked-out and not-allowed users get the same redirects as SignIn
        group.MapPost("SignInWithPasskey", async (HttpContext httpContext, IFormCollection fc,
            SignInManager<TUser> signInManager, UserManager<TUser> userManager) =>
        {
            var credentialJson = (string?)fc[PasskeyCredentialField];
            if (string.IsNullOrEmpty(credentialJson))
            {
                return Results.Redirect($"{routes.LoginRoute}?failed=passkey");
            }

            var result = await signInManager.PasskeySignInAsync(credentialJson);

            // A successful sign-in puts the new principal on HttpContext.User
            var user = result.Succeeded ? await userManager.GetUserAsync(httpContext.User) : null;
            LogSignInAttempt(httpContext, user?.UserName, result);

            if (user is not null)
            {
                return await RedirectAfterSignInAsync(user, httpContext);
            }

            return result switch
            {
                { IsLockedOut: true } => Results.Redirect(routes.LockoutRoute),
                { IsNotAllowed: true } => Results.Redirect($"{routes.LoginRoute}?failed=notallowed"),
                _ => Results.Redirect($"{routes.LoginRoute}?failed=passkey")
            };
        }).AllowAnonymous();

        // Registers the passkey PasskeySubmit created. A full request, because attestation reads the
        // challenge cookie that PasskeyCreationOptions set
        group.MapPost("AddPasskey", async (HttpContext httpContext, IFormCollection fc,
            IUserService userService, UserManager<TUser> userManager) =>
        {
            var userId = userManager.GetUserId(httpContext.User);
            var credentialJson = (string?)fc[PasskeyCredentialField];
            if (userId is null || string.IsNullOrEmpty(credentialJson))
            {
                return PasskeyRedirect("failed");
            }

            var passkeys = await userService.GetPasskeysAsync(userId);
            if (passkeys is null)
            {
                return PasskeyRedirect("failed");
            }

            if (passkeys.Count >= MaxPasskeys)
            {
                return PasskeyRedirect("limit");
            }

            PasskeyAttestationResult attestation;
            try
            {
                attestation = await userService.PerformPasskeyAttestationAsync(credentialJson);
            }
            catch (InvalidOperationException)
            {
                // No challenge cookie: the options were never fetched, or the cookie expired
                return PasskeyRedirect("failed");
            }

            if (!attestation.Succeeded)
            {
                GetLogger(httpContext).LogWarning("Passkey attestation failed: {Reason}", attestation.Failure.Message);
                return PasskeyRedirect("failed");
            }

            // The challenge names the user it was issued for; a different account signed in since then must not get the passkey
            if (attestation.UserEntity.Id != userId)
            {
                return PasskeyRedirect("failed");
            }

            if (PasskeyAuthenticators.TryGetDefaultDisplayName(attestation.Passkey, out var defaultName))
            {
                attestation.Passkey.Name = defaultName;
            }

            var addResult = await userService.AddOrUpdatePasskeyAsync(userId, attestation.Passkey);
            return PasskeyRedirect(addResult.Succeeded ? "added" : "failed");
        }).RequireAuthorization();

        IResult PasskeyRedirect(string status) => Results.Redirect($"{routes.PasskeysRoute}?passkey={status}");

        async Task<IResult> PasswordResultAsync(IdentityResult result, TUser user, SignInManager<TUser> signInManager, string failureRoute)
        {
            if (!result.Succeeded)
            {
                var code = result.Errors.FirstOrDefault()?.Code ?? "unknown";
                return Results.Redirect($"{failureRoute}?failed={Uri.EscapeDataString(code)}");
            }

            await signInManager.RefreshSignInAsync(user);
            return Results.Redirect(routes.HomeRoute);
        }

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

    private static readonly IdentityResult ConfirmationMismatch = IdentityResult.Failed(
        new IdentityError { Code = "ConfirmationMismatch", Description = "The password and its confirmation do not match." });

    // The confirmation field is optional so a form without one still posts
    private static bool ConfirmationMatches(IFormCollection fc, string newPassword) =>
        !fc.ContainsKey("ConfirmPassword") || (string?)fc["ConfirmPassword"] == newPassword;

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
