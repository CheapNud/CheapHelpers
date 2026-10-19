using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm;

/// <summary>
/// Picks the transport per call from the current <see cref="LlmOptions"/>: Anthropic with a key
/// goes to the native Messages API client, everything else to the OpenAI-compatible client.
/// </summary>
public sealed class RoutedLlmClient(
    IOptionsMonitor<LlmOptions> options,
    OpenAiCompatibleLlmClient compatible,
    AnthropicLlmClient native) : ILlmClient
{
    public Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default) =>
        Pick().GenerateAsync(request, cancellationToken);

    public IAsyncEnumerable<LlmStreamChunk> GenerateStreamAsync(LlmRequest request, CancellationToken cancellationToken = default) =>
        Pick().GenerateStreamAsync(request, cancellationToken);

    private ILlmClient Pick() => UseNative(options.CurrentValue) ? native : compatible;

    /// <summary>
    /// Native only with a real key: with an empty key the compatible client returns the
    /// provider's own error body instead of an SDK auth exception.
    /// </summary>
    internal static bool UseNative(LlmOptions config) =>
        config.Provider == LlmProvider.Anthropic && !string.IsNullOrWhiteSpace(config.ApiKey);
}
