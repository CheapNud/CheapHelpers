using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Llm;

public static class LlmServiceExtensions
{
    /// <summary>
    /// Registers <see cref="ILlmClient"/>, routed per call to the native Anthropic client or the
    /// OpenAI-compatible client, plus the shared <see cref="LlmUsageTracker"/>.
    /// </summary>
    public static IServiceCollection AddCheapLlm(this IServiceCollection services, Action<LlmOptions> configure)
    {
        services.Configure(configure);
        // The client enforces LlmOptions.TimeoutSeconds itself; HttpClient's own 100 s default
        // would otherwise cut off any longer setting.
        services.AddHttpClient<OpenAiCompatibleLlmClient>(httpClient => httpClient.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<LlmUsageTracker>();
        services.AddSingleton<AnthropicClientFactory>();
        services.AddSingleton<AnthropicLlmClient>();
        // Transient like the typed client it wraps, so the HttpClient handler keeps rotating.
        services.AddTransient<ILlmClient, RoutedLlmClient>();
        return services;
    }
}
