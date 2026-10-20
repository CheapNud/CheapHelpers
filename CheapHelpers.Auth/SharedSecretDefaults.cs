namespace CheapHelpers.Auth;

/// <summary>
/// Default values for <see cref="SharedSecretAuthenticationHandler"/>.
/// </summary>
public static class SharedSecretDefaults
{
    /// <summary>Authentication scheme name registered by <see cref="SharedSecretAuthenticationExtensions.AddSharedSecret"/>.</summary>
    public const string AuthenticationScheme = "SharedSecret";

    /// <summary>Default header that carries the secret.</summary>
    public const string HeaderName = "X-Shared-Secret";

    /// <summary>Default name of the authenticated principal.</summary>
    public const string PrincipalName = "shared-secret";
}
