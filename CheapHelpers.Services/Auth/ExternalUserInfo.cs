namespace CheapHelpers.Services.Auth;

/// <summary>
/// Provider-agnostic external user information for provisioning into the local Identity system.
/// </summary>
public sealed record ExternalUserInfo(
    string ProviderName,
    string ExternalId,
    string? Email = null,
    string? Username = null,
    string? AvatarUrl = null,
    Dictionary<string, string>? AdditionalClaims = null);
