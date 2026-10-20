using Microsoft.AspNetCore.Authentication;

namespace CheapHelpers.Auth;

/// <summary>
/// Registers <see cref="SharedSecretAuthenticationHandler"/>.
/// </summary>
public static class SharedSecretAuthenticationExtensions
{
    /// <summary>
    /// Adds the shared-secret scheme under <see cref="SharedSecretDefaults.AuthenticationScheme"/>.
    /// Protect endpoints with a policy that names that scheme and requires an authenticated user.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureOptions">Sets <see cref="SharedSecretOptions.Secret"/> and any overrides.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddSharedSecret(
        this AuthenticationBuilder builder,
        Action<SharedSecretOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return builder.AddScheme<SharedSecretOptions, SharedSecretAuthenticationHandler>(
            SharedSecretDefaults.AuthenticationScheme, configureOptions);
    }
}
