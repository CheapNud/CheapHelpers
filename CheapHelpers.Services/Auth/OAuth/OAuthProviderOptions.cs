namespace CheapHelpers.Services.Auth.OAuth;

/// <summary>
/// Base configuration for ASP.NET Core external OAuth providers (Google, Microsoft).
/// Shared options that mirror the relevant subset of <see cref="Plex.PlexAuthOptions"/>.
/// </summary>
public abstract class OAuthProviderOptions
{
    /// <summary>
    /// Provider name used for <see cref="IExternalAuthProvider"/> enumeration and <see cref="ExternalUserInfo"/>.
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// OAuth Client ID from the provider's developer console.
    /// </summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// OAuth Client Secret from the provider's developer console.
    /// </summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>
    /// Path for the start endpoint that issues the OAuth challenge.
    /// </summary>
    public abstract string StartPath { get; set; }

    /// <summary>
    /// Path for our callback endpoint that processes the external auth result.
    /// </summary>
    public abstract string CallbackPath { get; set; }

    /// <summary>
    /// Path to redirect unauthenticated users to (with optional ?error= query parameter).
    /// </summary>
    public string LoginPath { get; set; } = "/login";

    /// <summary>
    /// Path to redirect to after successful authentication.
    /// </summary>
    public string PostLoginRedirect { get; set; } = "/";

    /// <summary>
    /// Expiration for the primary authentication cookie.
    /// </summary>
    public TimeSpan CookieExpiration { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Additional OAuth scopes beyond the defaults (email, profile are always included).
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Optional authorization hook invoked after successful authentication.
    /// Return false to deny access. The <see cref="IServiceProvider"/> parameter allows resolving any service.
    /// </summary>
    public Func<ExternalUserInfo, IServiceProvider, CancellationToken, Task<bool>>? AuthorizeUser { get; set; }

    /// <summary>
    /// Optional factory to produce additional claims beyond the defaults (NameIdentifier, Name, Email, Avatar).
    /// Return key-value pairs where key is the claim type.
    /// </summary>
    public Func<ExternalUserInfo, IEnumerable<KeyValuePair<string, string>>>? AdditionalClaimsFactory { get; set; }
}
