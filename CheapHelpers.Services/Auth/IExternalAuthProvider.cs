namespace CheapHelpers.Services.Auth;

/// <summary>
/// Marker interface for external authentication providers.
/// Enables DI enumeration of registered providers via <c>IEnumerable&lt;IExternalAuthProvider&gt;</c>.
/// </summary>
public interface IExternalAuthProvider
{
    string ProviderName { get; }
}
