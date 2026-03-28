namespace CheapHelpers.Services.Auth.Plex;

/// <summary>
/// Configuration options for the Plex SSO authentication provider.
/// </summary>
public class PlexAuthOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Plex";

    /// <summary>
    /// Product name sent in the X-Plex-Product header. Identifies the application to Plex.
    /// </summary>
    public string ProductName { get; set; } = "CheapHelpers";

    /// <summary>
    /// Client identifier sent in the X-Plex-Client-Identifier header. Must be unique per application.
    /// </summary>
    public string ClientIdentifier { get; set; } = "CheapHelpers";

    /// <summary>
    /// Admin token for server access validation via <see cref="IPlexAuthService.HasServerAccessAsync"/>.
    /// Required only when using server-gated access. Set via user-secrets or environment variable.
    /// </summary>
    public string? AdminToken { get; set; }

    /// <summary>
    /// Path for the auth start endpoint that initiates the Plex OAuth flow.
    /// </summary>
    public string StartPath { get; set; } = "/auth/plex-start";

    /// <summary>
    /// Path for the callback endpoint that Plex redirects to after user authentication.
    /// </summary>
    public string CallbackPath { get; set; } = "/auth/plex-callback";

    /// <summary>
    /// Path for the logout endpoint.
    /// </summary>
    public string LogoutPath { get; set; } = "/auth/logout";

    /// <summary>
    /// Path to redirect unauthenticated users to (with optional ?error= query parameter).
    /// </summary>
    public string LoginPath { get; set; } = "/login";

    /// <summary>
    /// Path to redirect to after successful authentication.
    /// </summary>
    public string PostLoginRedirect { get; set; } = "/";

    /// <summary>
    /// Path to redirect to after logout.
    /// </summary>
    public string PostLogoutRedirect { get; set; } = "/";

    /// <summary>
    /// Override for the callback base URL. When null, auto-detected from the incoming request.
    /// Useful when behind a reverse proxy where the public URL differs from the internal one.
    /// </summary>
    public string? CallbackBaseUrl { get; set; }

    /// <summary>
    /// Lifetime of the temporary HTTP-only cookie that stores the PIN ID during the auth handshake.
    /// </summary>
    public TimeSpan PinCookieLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Number of times to poll the Plex PIN endpoint in the callback before giving up.
    /// CheapManga uses 1 (single check), CheapNights uses 5.
    /// </summary>
    public int PinPollAttempts { get; set; } = 1;

    /// <summary>
    /// Delay between PIN poll attempts.
    /// </summary>
    public TimeSpan PinPollDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Expiration for the authentication cookie.
    /// </summary>
    public TimeSpan CookieExpiration { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Optional authorization hook invoked after successful Plex authentication.
    /// Return false to deny access. When null, all authenticated Plex users are allowed.
    /// The <see cref="IServiceProvider"/> parameter allows resolving services like <see cref="IPlexAuthService"/>.
    /// </summary>
    public Func<PlexUser, IServiceProvider, CancellationToken, Task<bool>>? AuthorizeUser { get; set; }

    /// <summary>
    /// Optional factory to produce additional claims beyond the default set
    /// (NameIdentifier, Name, Email, Avatar). Return key-value pairs where key is the claim type.
    /// </summary>
    public Func<PlexUser, IEnumerable<KeyValuePair<string, string>>>? AdditionalClaimsFactory { get; set; }
}
