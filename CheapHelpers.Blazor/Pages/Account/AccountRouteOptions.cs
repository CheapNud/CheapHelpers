namespace CheapHelpers.Blazor.Pages.Account;

/// <summary>
/// Configurable route paths for the account controller.
/// Override these when the consumer maps the controller to a different base path.
/// </summary>
public class AccountRouteOptions
{
    public const string SectionName = "AccountRoutes";

    /// <summary>
    /// Base route prefix (default "Account"). Used by consumer's [Route] attribute.
    /// </summary>
    public string BaseRoute { get; set; } = "Account";

    public string HomeRoute { get; set; } = "/";
    public string LoginRoute { get; set; } = "/Account/Login/";
    public string LockoutRoute { get; set; } = "/Account/Lockout/";
    public string EnableAuthenticatorRoute { get; set; } = "/Account/EnableAuthenticator";

    /// <summary>
    /// TOTP challenge page shown when a password sign-in requires two-factor.
    /// </summary>
    public string TwoFactorRoute { get; set; } = "/Account/Login2FA";

    /// <summary>
    /// Recovery-code fallback page for users without their authenticator.
    /// </summary>
    public string RecoveryCodeRoute { get; set; } = "/Account/LoginWithRecoveryCode";
}
