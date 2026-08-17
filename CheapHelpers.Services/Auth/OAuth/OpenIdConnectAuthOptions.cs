namespace CheapHelpers.Services.Auth.OAuth;

/// <summary>
/// Configuration options for a generic OpenID Connect provider (Authelia, Keycloak, Authentik, ...).
/// Any standards-compliant OIDC IdP works — endpoints come from the authority's discovery document
/// at <c>{Authority}/.well-known/openid-configuration</c>.
/// </summary>
public class OpenIdConnectAuthOptions : OAuthProviderOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Oidc";

    public OpenIdConnectAuthOptions()
    {
        // OIDC defaults: email for provisioning, groups for authorization.
        // The handler always requests openid + profile. "groups" is not a standard OIDC
        // scope but is used by Authelia/Keycloak/Authentik — strict IdPs that reject
        // unknown scopes need Scopes.Remove("groups").
        Scopes = ["email", "groups"];
    }

    /// <summary>
    /// Display name used for <see cref="ExternalUserInfo"/>, logging and error messages (e.g. "Authelia").
    /// </summary>
    public string DisplayName { get; set; } = "OIDC";

    public override string ProviderName => DisplayName;
    public override string StartPath { get; set; } = "/auth/oidc-start";
    public override string CallbackPath { get; set; } = "/auth/oidc-callback";

    /// <summary>
    /// Issuer base URL, e.g. <c>https://auth.example.com</c>.
    /// </summary>
    public string Authority { get; set; } = "";

    /// <summary>
    /// Path the IdP redirects back to, handled internally by the OIDC middleware.
    /// Must match the redirect_uri registered on the IdP client (host + this path).
    /// </summary>
    public string RemoteCallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Authentication scheme name. When registering multiple OIDC providers side by side,
    /// give each a unique scheme AND a unique <see cref="RemoteCallbackPath"/>,
    /// <see cref="OAuthProviderOptions.StartPath"/> and <see cref="OAuthProviderOptions.CallbackPath"/>.
    /// </summary>
    public string AuthenticationScheme { get; set; } = "oidc";

    /// <summary>
    /// Claim used as the username (OIDC standard: preferred_username).
    /// </summary>
    public string UsernameClaim { get; set; } = "preferred_username";

    /// <summary>
    /// Claim holding group memberships. Groups are copied to role claims on the signed-in
    /// principal (so <c>[Authorize(Roles = ...)]</c> works) and joined into
    /// <see cref="ExternalUserInfo.AdditionalClaims"/> under "groups".
    /// </summary>
    public string GroupsClaim { get; set; } = "groups";

    /// <summary>
    /// Set false only for plain-HTTP IdPs on a trusted LAN.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
