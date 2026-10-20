using Microsoft.AspNetCore.Authentication;

namespace CheapHelpers.Auth;

/// <summary>
/// Options for <see cref="SharedSecretAuthenticationHandler"/>.
/// </summary>
public class SharedSecretOptions : AuthenticationSchemeOptions
{
    /// <summary>The secret callers must send. Required; an empty value fails at first use.</summary>
    public string Secret { get; set; } = "";

    /// <summary>Header that carries the secret.</summary>
    public string HeaderName { get; set; } = SharedSecretDefaults.HeaderName;

    /// <summary>Also accept the secret as <c>Authorization: Bearer &lt;secret&gt;</c>.</summary>
    public bool AllowBearer { get; set; } = true;

    /// <summary>Name claim given to the authenticated principal.</summary>
    public string PrincipalName { get; set; } = SharedSecretDefaults.PrincipalName;

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrEmpty(Secret))
            throw new InvalidOperationException($"{nameof(SharedSecretOptions)}.{nameof(Secret)} is required");

        if (string.IsNullOrWhiteSpace(HeaderName))
            throw new InvalidOperationException($"{nameof(SharedSecretOptions)}.{nameof(HeaderName)} is required");
    }
}
