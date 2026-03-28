namespace CheapHelpers.Services.Auth.OAuth;

/// <summary>
/// Configuration options for Microsoft OAuth authentication.
/// </summary>
public class MicrosoftAuthOptions : OAuthProviderOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Microsoft";

    public override string ProviderName => "Microsoft";
    public override string StartPath { get; set; } = "/auth/microsoft-start";
    public override string CallbackPath { get; set; } = "/auth/microsoft-callback";

    /// <summary>
    /// Azure AD tenant ID. Controls which Microsoft accounts can sign in.
    /// Defaults to "common" (any Microsoft account or organizational account).
    /// Set to a specific tenant GUID for single-org, or "consumers" for personal accounts only.
    /// </summary>
    public string TenantId { get; set; } = "common";
}
