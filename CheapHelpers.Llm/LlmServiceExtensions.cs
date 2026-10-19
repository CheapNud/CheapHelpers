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
        services.AddHttpClient<OpenAiCompatibleLlmClient>();
        services.AddSingleton<LlmUsageTracker>();
        services.AddSingleton<AnthropicClientFactory>();
        services.AddSingleton<AnthropicLlmClient>();
        // Transient like the typed client it wraps, so the HttpClient handler keeps rotating.
        services.AddTransient<ILlmClient, RoutedLlmClient>();
        return services;
    }
}
