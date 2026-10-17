namespace CheapHelpers.Blazor.Pages.Account;

/// <summary>
/// Configurable route paths for the account controller.
/// Override these when the consumer maps the controller to a different base path.
/// </summary>
public class AccountRouteOptions
{
    public const string SectionName = "AccountRoutes";

    /// <summary>
    /// Base route prefix (default "Account").
    /// </summary>
    public string BaseRoute { get; set; } = "Account";

    /// <summary>
    /// Path of an endpoint mapped by MapCheapAccountEndpoints, e.g. <c>EndpointPath("SignOut")</c> gives <c>/Account/SignOut</c>.
    /// </summary>
    public string EndpointPath(string name) => $"/{BaseRoute.Trim('/')}/{name}";

    public string HomeRoute { get; set; } = "/";
    public string LoginRoute { get; set; } = "/Account/Login/";
    public string LockoutRoute { get; set; } = "/Account/Lockout/";
    public string EnableAuthenticatorRoute { get; set; } = "/Account/EnableAuthenticator";
    public string ChangePasswordRoute { get; set; } = "/Account/ChangePassword";
    public string SetPasswordRoute { get; set; } = "/Account/SetPassword";

    /// <summary>
    /// Account page tab that lists the passkeys (the Security tab). AddPasskey redirects here
    /// with <c>?passkey=added|limit|failed</c>.
    /// </summary>
    public string PasskeysRoute { get; set; } = "/Account/1";

    /// <summary>
    /// TOTP challenge page shown when a password sign-in requires two-factor.
    /// </summary>
    public string TwoFactorRoute { get; set; } = "/Account/Login2FA";

    /// <summary>
    /// Recovery-code fallback page for users without their authenticator.
    /// </summary>
    public string RecoveryCodeRoute { get; set; } = "/Account/LoginWithRecoveryCode";

    /// <summary>
    /// Recovery codes page, shown after the authenticator is enabled.
    /// </summary>
    public string RecoveryCodesRoute { get; set; } = "/Account/RecoveryCodes";
}
