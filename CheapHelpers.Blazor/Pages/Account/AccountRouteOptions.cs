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
    /// Whether to automatically map the account controller endpoints via MapControllers().
    /// Set to false if the consumer registers their own derived controller.
    /// </summary>
    public bool AutoMapEndpoints { get; set; } = true;
}
