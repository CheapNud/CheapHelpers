using System.Security.Claims;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Auth;
using CheapHelpers.Services.Auth.Plex;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Extension methods for mapping Plex SSO authentication endpoints.
/// </summary>
public static class PlexAuthBlazorExtensions
{
    private const string PinCookieName = "plex-pin-id";

    /// <summary>
    /// Maps the Plex SSO authentication endpoints: start, callback, and logout.
    /// Paths are configurable via <see cref="PlexAuthOptions"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapPlexAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var plexOptions = endpoints.ServiceProvider.GetRequiredService<PlexAuthOptions>();

        endpoints.MapGet(plexOptions.StartPath, async (IPlexAuthService plexAuth, HttpContext httpContext) =>
        {
            var baseUrl = plexOptions.CallbackBaseUrl
                ?? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            var (pinId, pinCode) = await plexAuth.CreatePinAsync(httpContext.RequestAborted);
            var authUrl = plexAuth.GetAuthRedirectUrl(pinCode, $"{baseUrl}{plexOptions.CallbackPath}");

            httpContext.Response.Cookies.Append(PinCookieName, pinId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = plexOptions.PinCookieLifetime,
            });

            return Results.Redirect(authUrl);
        }).AllowAnonymous();

        endpoints.MapGet(plexOptions.CallbackPath, async (IPlexAuthService plexAuth, HttpContext httpContext) =>
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<PlexAuthService>>();

            if (!httpContext.Request.Cookies.TryGetValue(PinCookieName, out var pinIdStr) || !long.TryParse(pinIdStr, out var pinId))
                return Results.Redirect($"{plexOptions.LoginPath}?error={Uri.EscapeDataString("Authentication session expired. Please try again.")}");

            httpContext.Response.Cookies.Delete(PinCookieName);

            // Poll the PIN up to PinPollAttempts times
            PlexPin? authenticatedPin = null;
            for (var attempt = 0; attempt < plexOptions.PinPollAttempts; attempt++)
            {
                authenticatedPin = await plexAuth.CheckPinAsync(pinId, httpContext.RequestAborted);
                if (authenticatedPin is not null)
                    break;

                if (attempt < plexOptions.PinPollAttempts - 1)
                    await Task.Delay(plexOptions.PinPollDelay, httpContext.RequestAborted);
            }

            if (authenticatedPin?.AuthToken is null)
                return Results.Redirect($"{plexOptions.LoginPath}?error={Uri.EscapeDataString("Plex authentication timed out. Please try again.")}");

            var plexUser = await plexAuth.GetUserAsync(authenticatedPin.AuthToken, httpContext.RequestAborted);
            if (plexUser is null)
                return Results.Redirect($"{plexOptions.LoginPath}?error={Uri.EscapeDataString("Failed to retrieve Plex user info.")}");

            // Run optional authorization hook
            if (plexOptions.AuthorizeUser is not null)
            {
                var authorized = await plexOptions.AuthorizeUser(plexUser, httpContext.RequestServices, httpContext.RequestAborted);
                if (!authorized)
                {
                    logger.LogWarning("Plex user {Username} (ID: {PlexId}) denied access by AuthorizeUser hook", plexUser.Username, plexUser.Id);
                    return Results.Redirect($"{plexOptions.LoginPath}?error={Uri.EscapeDataString($"Sorry {plexUser.Username}, you don't have access.")}");
                }
            }

            // Build claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, plexUser.Id.ToString()),
                new(ClaimTypes.Name, plexUser.Username),
            };

            if (plexUser.Email is not null)
                claims.Add(new Claim(ClaimTypes.Email, plexUser.Email));

            if (plexUser.Thumb is not null)
                claims.Add(new Claim("Avatar", plexUser.Thumb));

            // Add any additional claims from the factory
            if (plexOptions.AdditionalClaimsFactory is not null)
            {
                foreach (var kvp in plexOptions.AdditionalClaimsFactory(plexUser))
                    claims.Add(new Claim(kvp.Key, kvp.Value));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(plexOptions.CookieExpiration),
                });

            // Optional: provision or link a CheapUser via IExternalUserProvisioner (only if registered)
            var provisioner = httpContext.RequestServices.GetService<IExternalUserProvisioner>();
            if (provisioner is not null)
            {
                var userInfo = new ExternalUserInfo(
                    ProviderName: "Plex",
                    ExternalId: plexUser.Id.ToString(),
                    Email: plexUser.Email,
                    Username: plexUser.Username,
                    AvatarUrl: plexUser.Thumb);

                var provisionResult = await provisioner.FindOrCreateUserAsync(userInfo, httpContext.RequestAborted);
                if (provisionResult is { Success: true, SignInRequired: true })
                {
                    var signInManager = httpContext.RequestServices.GetRequiredService<SignInManager<CheapUser>>();
                    var identityUserManager = httpContext.RequestServices.GetRequiredService<UserManager<CheapUser>>();
                    var identityUser = await identityUserManager.FindByIdAsync(provisionResult.UserId!);
                    if (identityUser is not null)
                    {
                        await signInManager.SignInAsync(identityUser, isPersistent: true);
                        logger.LogInformation("Plex user {Username} (ID: {PlexId}) provisioned and signed in via Identity", plexUser.Username, plexUser.Id);
                        return Results.Redirect(plexOptions.PostLoginRedirect);
                    }
                }
            }

            logger.LogInformation("Plex user {Username} (ID: {PlexId}) signed in successfully", plexUser.Username, plexUser.Id);
            return Results.Redirect(plexOptions.PostLoginRedirect);
        }).AllowAnonymous();

        endpoints.MapGet(plexOptions.LogoutPath, async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect(plexOptions.PostLogoutRedirect);
        }).AllowAnonymous();

        return endpoints;
    }
}
