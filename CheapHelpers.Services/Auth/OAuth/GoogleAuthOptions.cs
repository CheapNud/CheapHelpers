namespace CheapHelpers.Services.Auth.OAuth;

/// <summary>
/// Configuration options for Google OAuth authentication.
/// </summary>
public class GoogleAuthOptions : OAuthProviderOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Google";

    public override string ProviderName => "Google";
    public override string StartPath { get; set; } = "/auth/google-start";
    public override string CallbackPath { get; set; } = "/auth/google-callback";

    /// <summary>
    /// Restrict login to a specific Google Workspace domain (e.g., "example.com").
    /// When set, only users from this domain can authenticate. Enforced both client-side
    /// (via the <c>hd</c> parameter) and server-side (email domain verification).
    /// Null allows any Google account.
    /// </summary>
    public string? HostedDomain { get; set; }
}
