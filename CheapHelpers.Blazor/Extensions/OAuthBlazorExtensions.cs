using System.Security.Claims;
using AspNet.Security.OAuth.Apple;
using AspNet.Security.OAuth.GitHub;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Auth;
using CheapHelpers.Services.Auth.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Extension methods for registering OAuth authentication providers (Google, Microsoft, GitHub, Apple).
/// Wraps ASP.NET Core's built-in and aspnet-contrib OAuth handlers.
/// </summary>
public static class OAuthBlazorExtensions
{
    private const string ExternalCookieScheme = "ExternalCookie";

    /// <summary>
    /// Adds Google OAuth authentication. Configures ASP.NET Core's built-in Google handler
    /// with a temporary external cookie scheme and registers <see cref="IExternalAuthProvider"/>.
    /// </summary>
    public static IServiceCollection AddGoogleAuth(
        this IServiceCollection services,
        Action<GoogleAuthOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var googleOptions = new GoogleAuthOptions();
        configureOptions(googleOptions);

        services.AddSingleton(googleOptions);
        services.AddSingleton<IExternalAuthProvider>(new SimpleAuthProvider("Google"));

        EnsureExternalCookieScheme(services);

        services.AddAuthentication()
            .AddGoogle(GoogleDefaults.AuthenticationScheme, opt =>
            {
                opt.ClientId = googleOptions.ClientId;
                opt.ClientSecret = googleOptions.ClientSecret;
                opt.SignInScheme = ExternalCookieScheme;
                opt.CallbackPath = "/signin-google";

                foreach (var scope in googleOptions.Scopes)
                    opt.Scope.Add(scope);

                if (googleOptions.HostedDomain is not null)
                {
                    opt.Events.OnRedirectToAuthorizationEndpoint = ctx =>
                    {
                        ctx.Response.Redirect(ctx.RedirectUri + "&hd=" + Uri.EscapeDataString(googleOptions.HostedDomain));
                        return Task.CompletedTask;
                    };
                }
            });

        return services;
    }

    /// <summary>
    /// Adds Microsoft OAuth authentication. Configures ASP.NET Core's built-in Microsoft Account handler
    /// with a temporary external cookie scheme and registers <see cref="IExternalAuthProvider"/>.
    /// </summary>
    public static IServiceCollection AddMicrosoftAuth(
        this IServiceCollection services,
        Action<MicrosoftAuthOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var microsoftOptions = new MicrosoftAuthOptions();
        configureOptions(microsoftOptions);

        services.AddSingleton(microsoftOptions);
        services.AddSingleton<IExternalAuthProvider>(new SimpleAuthProvider("Microsoft"));

        EnsureExternalCookieScheme(services);

        services.AddAuthentication()
            .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, opt =>
            {
                opt.ClientId = microsoftOptions.ClientId;
                opt.ClientSecret = microsoftOptions.ClientSecret;
                opt.SignInScheme = ExternalCookieScheme;
                opt.CallbackPath = "/signin-microsoft";

                opt.AuthorizationEndpoint = $"https://login.microsoftonline.com/{microsoftOptions.TenantId}/oauth2/v2.0/authorize";
                opt.TokenEndpoint = $"https://login.microsoftonline.com/{microsoftOptions.TenantId}/oauth2/v2.0/token";

                foreach (var scope in microsoftOptions.Scopes)
                    opt.Scope.Add(scope);
            });

        return services;
    }

    /// <summary>
    /// Adds GitHub OAuth authentication using the aspnet-contrib GitHub handler.
    /// Registers <see cref="IExternalAuthProvider"/> and maps the <c>user:email</c> scope by default.
    /// </summary>
    public static IServiceCollection AddGitHubAuth(
        this IServiceCollection services,
        Action<GitHubAuthOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var githubOptions = new GitHubAuthOptions();
        configureOptions(githubOptions);

        services.AddSingleton(githubOptions);
        services.AddSingleton<IExternalAuthProvider>(new SimpleAuthProvider("GitHub"));

        EnsureExternalCookieScheme(services);

        services.AddAuthentication()
            .AddGitHub(GitHubAuthenticationDefaults.AuthenticationScheme, opt =>
            {
                opt.ClientId = githubOptions.ClientId;
                opt.ClientSecret = githubOptions.ClientSecret;
                opt.SignInScheme = ExternalCookieScheme;
                opt.CallbackPath = "/signin-github";
                opt.Scope.Add("user:email");

                if (githubOptions.EnterpriseDomain is not null)
                    opt.EnterpriseDomain = githubOptions.EnterpriseDomain;

                // Map avatar_url from GitHub's user API response
                opt.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

                foreach (var scope in githubOptions.Scopes)
                    opt.Scope.Add(scope);
            });

        return services;
    }

    /// <summary>
    /// Adds Apple Sign In authentication using the aspnet-contrib Apple handler.
    /// Registers <see cref="IExternalAuthProvider"/> and configures JWT client secret generation.
    /// </summary>
    /// <remarks>
    /// Requires an Apple Developer account with Sign In with Apple configured.
    /// The private key (.p8 file) must be provided via <see cref="AppleAuthOptions.PrivateKeyPath"/>
    /// or <see cref="AppleAuthOptions.PrivateKeyContent"/>.
    /// Apple only returns name/email on the user's FIRST sign-in.
    /// </remarks>
    public static IServiceCollection AddAppleAuth(
        this IServiceCollection services,
        Action<AppleAuthOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var appleOptions = new AppleAuthOptions();
        configureOptions(appleOptions);

        services.AddSingleton(appleOptions);
        services.AddSingleton<IExternalAuthProvider>(new SimpleAuthProvider("Apple"));

        EnsureExternalCookieScheme(services);

        services.AddAuthentication()
            .AddApple(AppleAuthenticationDefaults.AuthenticationScheme, opt =>
            {
                opt.ClientId = appleOptions.ServiceId ?? appleOptions.ClientId;
                opt.SignInScheme = ExternalCookieScheme;
                opt.CallbackPath = "/signin-apple";
                opt.KeyId = appleOptions.KeyId;
                opt.TeamId = appleOptions.TeamId;
                opt.GenerateClientSecret = true;

                // Configure private key source
                if (appleOptions.PrivateKeyContent is not null)
                {
                    var keyContent = appleOptions.PrivateKeyContent;
                    opt.PrivateKey = (_, _) => Task.FromResult<ReadOnlyMemory<char>>(keyContent.AsMemory());
                }
                else if (appleOptions.PrivateKeyPath is not null)
                {
                    var keyPath = appleOptions.PrivateKeyPath;
                    opt.PrivateKey = async (_, ct) =>
                    {
                        var content = await File.ReadAllTextAsync(keyPath, ct);
                        return content.AsMemory();
                    };
                }

                foreach (var scope in appleOptions.Scopes)
                    opt.Scope.Add(scope);
            });

        return services;
    }

    /// <summary>
    /// Maps OAuth start and callback endpoints for all registered OAuth providers (Google, Microsoft, GitHub, Apple).
    /// Logout is shared with Plex's existing logout endpoint — no duplicate needed.
    /// </summary>
    public static IEndpointRouteBuilder MapOAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapProviderEndpoints(endpoints,
            endpoints.ServiceProvider.GetService<GoogleAuthOptions>(),
            GoogleDefaults.AuthenticationScheme);

        MapProviderEndpoints(endpoints,
            endpoints.ServiceProvider.GetService<MicrosoftAuthOptions>(),
            MicrosoftAccountDefaults.AuthenticationScheme);

        MapProviderEndpoints(endpoints,
            endpoints.ServiceProvider.GetService<GitHubAuthOptions>(),
            GitHubAuthenticationDefaults.AuthenticationScheme);

        MapProviderEndpoints(endpoints,
            endpoints.ServiceProvider.GetService<AppleAuthOptions>(),
            AppleAuthenticationDefaults.AuthenticationScheme);

        return endpoints;
    }

    private static void MapProviderEndpoints(
        IEndpointRouteBuilder endpoints,
        OAuthProviderOptions? providerOptions,
        string authenticationScheme)
    {
        if (providerOptions is null)
            return;

        endpoints.MapGet(providerOptions.StartPath, (HttpContext httpContext) =>
        {
            var properties = new AuthenticationProperties { RedirectUri = providerOptions.CallbackPath };
            return Results.Challenge(properties, [authenticationScheme]);
        }).AllowAnonymous();

        endpoints.MapGet(providerOptions.CallbackPath, (HttpContext httpContext) =>
            HandleOAuthCallbackAsync(httpContext, providerOptions))
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleOAuthCallbackAsync(
        HttpContext httpContext,
        OAuthProviderOptions providerOptions)
    {
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CheapHelpers.Auth.OAuth");

        // Read the external auth result from the temporary cookie
        var authResult = await httpContext.AuthenticateAsync(ExternalCookieScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            logger.LogWarning("{Provider} OAuth callback failed: {Error}", providerOptions.ProviderName, authResult.Failure?.Message);
            return Results.Redirect($"{providerOptions.LoginPath}?error={Uri.EscapeDataString($"{providerOptions.ProviderName} authentication failed. Please try again.")}");
        }

        var externalPrincipal = authResult.Principal;

        // Extract claims from the external principal
        var externalId = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var displayName = externalPrincipal.FindFirstValue(ClaimTypes.Name);
        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);
        var avatarUrl = externalPrincipal.FindFirstValue("picture") // Google
            ?? externalPrincipal.FindFirstValue("urn:google:picture") // Google fallback
            ?? externalPrincipal.FindFirstValue("urn:github:avatar"); // GitHub

        if (string.IsNullOrEmpty(externalId))
        {
            logger.LogWarning("{Provider} OAuth: no NameIdentifier claim found", providerOptions.ProviderName);
            return Results.Redirect($"{providerOptions.LoginPath}?error={Uri.EscapeDataString("Authentication failed — no user identifier received.")}");
        }

        // Google: server-side hosted domain enforcement
        if (providerOptions is GoogleAuthOptions { HostedDomain: not null } googleOpts && email is not null)
        {
            if (!email.EndsWith($"@{googleOpts.HostedDomain}", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Google OAuth: {Email} rejected — not in hosted domain {Domain}", email, googleOpts.HostedDomain);
                await httpContext.SignOutAsync(ExternalCookieScheme);
                return Results.Redirect($"{providerOptions.LoginPath}?error={Uri.EscapeDataString($"Only @{googleOpts.HostedDomain} accounts are allowed.")}");
            }
        }

        // Build ExternalUserInfo
        var userInfo = new ExternalUserInfo(
            ProviderName: providerOptions.ProviderName,
            ExternalId: externalId,
            Email: email,
            Username: displayName,
            AvatarUrl: avatarUrl);

        // Run optional authorization hook
        if (providerOptions.AuthorizeUser is not null)
        {
            var authorized = await providerOptions.AuthorizeUser(userInfo, httpContext.RequestServices, httpContext.RequestAborted);
            if (!authorized)
            {
                logger.LogWarning("{Provider} user {Name} ({Id}) denied access by AuthorizeUser hook",
                    providerOptions.ProviderName, displayName, externalId);
                await httpContext.SignOutAsync(ExternalCookieScheme);
                return Results.Redirect($"{providerOptions.LoginPath}?error={Uri.EscapeDataString($"Sorry {displayName ?? "user"}, you don't have access.")}");
            }
        }

        // Build primary claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, externalId),
            new(ClaimTypes.Name, displayName ?? ""),
        };

        if (email is not null)
            claims.Add(new Claim(ClaimTypes.Email, email));

        if (avatarUrl is not null)
            claims.Add(new Claim("Avatar", avatarUrl));

        if (providerOptions.AdditionalClaimsFactory is not null)
        {
            foreach (var kvp in providerOptions.AdditionalClaimsFactory(userInfo))
                claims.Add(new Claim(kvp.Key, kvp.Value));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign into primary cookie scheme and clean up external cookie
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(providerOptions.CookieExpiration),
            });

        await httpContext.SignOutAsync(ExternalCookieScheme);

        // Optional: provision or link a CheapUser via IExternalUserProvisioner (only if registered)
        var provisioner = httpContext.RequestServices.GetService<IExternalUserProvisioner>();
        if (provisioner is not null)
        {
            var provisionResult = await provisioner.FindOrCreateUserAsync(userInfo, httpContext.RequestAborted);
            if (provisionResult is { Success: true, SignInRequired: true })
            {
                var signInManager = httpContext.RequestServices.GetRequiredService<SignInManager<CheapUser>>();
                var identityUserManager = httpContext.RequestServices.GetRequiredService<UserManager<CheapUser>>();
                var identityUser = await identityUserManager.FindByIdAsync(provisionResult.UserId!);
                if (identityUser is not null)
                {
                    await signInManager.SignInAsync(identityUser, isPersistent: true);
                    logger.LogInformation("{Provider} user {Name} ({Id}) provisioned and signed in via Identity",
                        providerOptions.ProviderName, displayName, externalId);
                    return Results.Redirect(providerOptions.PostLoginRedirect);
                }
            }
        }

        logger.LogInformation("{Provider} user {Name} ({Id}) signed in successfully",
            providerOptions.ProviderName, displayName, externalId);
        return Results.Redirect(providerOptions.PostLoginRedirect);
    }

    private static void EnsureExternalCookieScheme(IServiceCollection services)
    {
        // Use a marker type to avoid double-registering the external cookie scheme
        if (services.Any(s => s.ServiceType == typeof(ExternalCookieMarker)))
            return;

        services.AddSingleton<ExternalCookieMarker>();
        services.AddAuthentication()
            .AddCookie(ExternalCookieScheme, opt =>
            {
                opt.Cookie.Name = ".CheapHelpers.External";
                opt.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });
    }

    private sealed class ExternalCookieMarker;

    private sealed class SimpleAuthProvider(string providerName) : IExternalAuthProvider
    {
        public string ProviderName => providerName;
    }
}
