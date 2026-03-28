namespace CheapHelpers.Services.Auth.OAuth;

/// <summary>
/// Configuration options for Apple Sign In authentication.
/// Apple requires a JWT client secret generated from an ES256 private key (.p8 file).
/// The library handles JWT generation automatically — provide the key via
/// <see cref="PrivateKeyPath"/> or <see cref="PrivateKeyContent"/>.
/// </summary>
/// <remarks>
/// Apple only returns the user's name and email on the FIRST sign-in.
/// Subsequent logins only provide the user identifier. Persist user info
/// via <see cref="IExternalUserProvisioner"/> on first login.
/// </remarks>
public class AppleAuthOptions : OAuthProviderOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Apple";

    public override string ProviderName => "Apple";
    public override string StartPath { get; set; } = "/auth/apple-start";
    public override string CallbackPath { get; set; } = "/auth/apple-callback";

    /// <summary>
    /// Apple Developer Team ID (10-character string from the Apple Developer portal).
    /// </summary>
    public string TeamId { get; set; } = "";

    /// <summary>
    /// Key ID for the Sign In with Apple private key (from the Apple Developer portal).
    /// </summary>
    public string KeyId { get; set; } = "";

    /// <summary>
    /// Apple Services ID. Defaults to <see cref="OAuthProviderOptions.ClientId"/> if not set.
    /// Apple uses "Services ID" where other providers use "Client ID".
    /// </summary>
    public string? ServiceId { get; set; }

    /// <summary>
    /// File path to the Apple .p8 private key file.
    /// Mutually exclusive with <see cref="PrivateKeyContent"/>.
    /// </summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>
    /// Raw PEM content of the Apple private key.
    /// Use this for non-file-based sources (environment variables, Azure Key Vault, etc.).
    /// Mutually exclusive with <see cref="PrivateKeyPath"/>.
    /// </summary>
    public string? PrivateKeyContent { get; set; }
}
