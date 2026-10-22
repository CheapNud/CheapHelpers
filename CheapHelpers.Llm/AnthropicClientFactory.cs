using Anthropic;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm;

/// <summary>
/// Builds one <see cref="AnthropicClient"/> per (key, base URL, timeout, retries) set and hands
/// the same instance back until one of them changes in <see cref="LlmOptions"/>. The SDK client
/// owns its own connection pool, so one per credential is the right granularity.
/// Without an injected <see cref="HttpClient"/> the factory owns every client it builds: a replaced
/// client is disposed when its successor is built (a request still running on it is cancelled), and
/// the current one when the factory is disposed.
/// An injected <see cref="HttpClient"/> stays with the caller, so those clients are never disposed here.
/// </summary>
public sealed class AnthropicClientFactory(IOptionsMonitor<LlmOptions> options, HttpClient? httpClient = null) : IDisposable
{
    private const string DefaultBaseUrl = "https://api.anthropic.com";
    private const string VersionSuffix = "/v1";

    private readonly Lock _lock = new();
    private (string ApiKey, string BaseUrl, int TimeoutSeconds, int MaxRetries) _builtFor;
    private IAnthropicClient? _client;

    /// <summary>Returns the client for the current options, building a new one when they changed.</summary>
    public IAnthropicClient Get()
    {
        var config = options.CurrentValue;
        var credential = (config.ApiKey, config.BaseUrl, config.TimeoutSeconds, config.MaxRetries);
        lock (_lock)
        {
            if (_client is not null && _builtFor == credential)
                return _client;

            var client = new AnthropicClient
            {
                ApiKey = credential.ApiKey,
                // The presets carry a trailing "/v1"; the SDK appends its own.
                BaseUrl = NormalizeBaseUrl(credential.BaseUrl),
                Timeout = TimeSpan.FromSeconds(credential.TimeoutSeconds),
                MaxRetries = credential.MaxRetries,
            };
            var replaced = _client;
            // The SDK's HttpClient property is non-nullable, so it is only overridden when one was given.
            _client = httpClient is null ? client : client.WithOptions(clientOptions => clientOptions with { HttpClient = httpClient });
            _builtFor = credential;
            DisposeIfOwned(replaced);
            return _client;
        }
    }

    /// <summary>Disposes the current client when the factory owns it.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            DisposeIfOwned(_client);
            _client = null;
        }
    }

    // The SDK client disposes the HttpClient it sends through, which would break an injected one
    // still shared with the caller and the replacement client
    private void DisposeIfOwned(IAnthropicClient? client)
    {
        if (httpClient is null)
            client?.Dispose();
    }

    internal static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return DefaultBaseUrl;
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith(VersionSuffix, StringComparison.OrdinalIgnoreCase) ? trimmed[..^VersionSuffix.Length] : trimmed;
    }
}
