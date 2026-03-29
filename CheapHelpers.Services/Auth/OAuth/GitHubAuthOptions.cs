namespace CheapHelpers.Services.Auth.OAuth;

/// <summary>
/// Configuration options for GitHub OAuth authentication.
/// </summary>
public class GitHubAuthOptions : OAuthProviderOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "GitHub";

    public override string ProviderName => "GitHub";
    public override string StartPath { get; set; } = "/auth/github-start";
    public override string CallbackPath { get; set; } = "/auth/github-callback";

    /// <summary>
    /// GitHub Enterprise domain for on-premises deployments (e.g., "github.mycompany.com").
    /// When set, authorization and token endpoints use this domain instead of github.com.
    /// Null uses public GitHub.
    /// </summary>
    public string? EnterpriseDomain { get; set; }
}
