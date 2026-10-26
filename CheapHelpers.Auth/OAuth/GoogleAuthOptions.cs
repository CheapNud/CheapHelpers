namespace CheapHelpers.Auth.OAuth;

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
    /// When set, the sign-in is accepted only when the account's <c>hd</c> claim matches this domain
    /// and the email is at this domain. The <c>hd</c> parameter on the redirect only pre-selects
    /// the account on Google's sign-in screen. Null allows any Google account.
    /// </summary>
    public string? HostedDomain { get; set; }
}
