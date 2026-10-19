using Anthropic;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm;

/// <summary>
/// Builds one <see cref="AnthropicClient"/> per (key, base URL, timeout, retries) set and hands
/// the same instance back until one of them changes in <see cref="LlmOptions"/>. The SDK client
/// owns its own connection pool, so one per credential is the right granularity.
/// </summary>
public sealed class AnthropicClientFactory(IOptionsMonitor<LlmOptions> options, HttpClient? httpClient = null)
{
    private const string DefaultBaseUrl = "https://api.anthropic.com";
    private const string VersionSuffix = "/v1";

    private readonly Lock _lock = new();
    private (string ApiKey, string BaseUrl, int TimeoutSeconds, int MaxRetries) _builtFor;
    private IAnthropicClient? _client;

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
            // The SDK's HttpClient property is non-nullable, so it is only overridden when one was given.
            _client = httpClient is null ? client : client.WithOptions(clientOptions => clientOptions with { HttpClient = httpClient });
            _builtFor = credential;
            return _client;
        }
    }

    internal static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return DefaultBaseUrl;
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith(VersionSuffix, StringComparison.OrdinalIgnoreCase) ? trimmed[..^VersionSuffix.Length] : trimmed;
    }
}
