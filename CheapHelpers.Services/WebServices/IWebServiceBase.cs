using Microsoft.AspNetCore.SignalR.Client;

namespace CheapHelpers.WebServices;

public interface IWebServiceBase : IAsyncDisposable
{
    string ServiceName { get; }
    HubConnection? HubConnection { get; }
    string Endpoint { get; }
    string HubUrl { get; }
    string ApiEndpoint { get; }
    string ApiUrl { get; }

    // SignalR Management
    Task StartHubAsync(CancellationToken cancellationToken = default);
    Task StopHubAsync(CancellationToken cancellationToken = default);

    // HTTP Methods
    Task<TResponse> GetAsync<TResponse>(string endpoint = "", CancellationToken cancellationToken = default) where TResponse : class;
    Task<TResponse> PostAsync<TResponse>(object? content = null, string endpoint = "", CancellationToken cancellationToken = default) where TResponse : class;
    Task<TResponse> PutAsync<TResponse>(object? content = null, string endpoint = "", CancellationToken cancellationToken = default) where TResponse : class;
    Task<TResponse> PatchAsync<TResponse>(object? content = null, string endpoint = "", CancellationToken cancellationToken = default) where TResponse : class;
    Task<TResponse> DeleteAsync<TResponse>(string endpoint = "", CancellationToken cancellationToken = default) where TResponse : class;
    Task DeleteAsync(string endpoint = "", CancellationToken cancellationToken = default);

    // Authentication
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task SetBearerTokenAsync(CancellationToken cancellationToken = default);
}