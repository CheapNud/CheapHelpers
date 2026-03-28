namespace CheapHelpers.Services.Auth;

/// <summary>
/// Finds or creates a local Identity user from external authentication provider data.
/// Register this service to opt into automatic user provisioning from external auth providers.
/// </summary>
public interface IExternalUserProvisioner
{
    Task<ExternalProvisionResult> FindOrCreateUserAsync(ExternalUserInfo userInfo, CancellationToken ct = default);
}
