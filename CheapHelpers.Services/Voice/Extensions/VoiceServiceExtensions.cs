using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Voice.Extensions;

/// <summary>
/// Registers the speech-to-text and text-to-speech providers. The first provider added for an interface becomes its default;
/// the concrete types stay injectable so a consumer can route between providers.
/// </summary>
public static class VoiceServiceExtensions
{
    /// <summary>
    /// Registers <see cref="WhisperSpeechToText"/> with a typed <see cref="HttpClient"/> and, if none is registered yet, as <see cref="ISpeechToText"/>.
    /// </summary>
    public static IServiceCollection AddWhisperSpeechToText(this IServiceCollection services, Action<WhisperSttOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddHttpClient<WhisperSpeechToText>();
        services.TryAddSingleton<ISpeechToText>(sp => sp.GetRequiredService<WhisperSpeechToText>());
        return services;
    }

    /// <summary>
    /// Registers <see cref="AzureSpeechToText"/> with a typed <see cref="HttpClient"/> and, if none is registered yet, as <see cref="ISpeechToText"/>.
    /// </summary>
    public static IServiceCollection AddAzureSpeechToText(this IServiceCollection services, Action<AzureSpeechOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddHttpClient<AzureSpeechToText>();
        services.TryAddSingleton<ISpeechToText>(sp => sp.GetRequiredService<AzureSpeechToText>());
        return services;
    }

    /// <summary>
    /// Registers <see cref="EdgeTextToSpeech"/> and <see cref="EdgeTtsVoiceCatalog"/> as singletons and, if none is registered yet,
    /// <see cref="ITextToSpeech"/> and <see cref="TimeProvider.System"/>.
    /// </summary>
    public static IServiceCollection AddEdgeTextToSpeech(this IServiceCollection services, Action<EdgeTtsOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<EdgeTextToSpeech>();
        // A typed client (AddHttpClient<T>) is transient, which would drop the catalog's cache on every resolve,
        // so the catalog is a singleton fed from a named client instead.
        services.AddHttpClient(nameof(EdgeTtsVoiceCatalog));
        services.TryAddSingleton(sp => new EdgeTtsVoiceCatalog(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(EdgeTtsVoiceCatalog)),
            sp.GetRequiredService<ILogger<EdgeTtsVoiceCatalog>>()));
        services.TryAddSingleton<ITextToSpeech>(sp => sp.GetRequiredService<EdgeTextToSpeech>());
        return services;
    }

    /// <summary>
    /// Registers <see cref="AzureTextToSpeech"/> with a typed <see cref="HttpClient"/> and, if none is registered yet, as <see cref="ITextToSpeech"/>.
    /// </summary>
    public static IServiceCollection AddAzureTextToSpeech(this IServiceCollection services, Action<AzureSpeechOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddHttpClient<AzureTextToSpeech>();
        services.TryAddSingleton<ITextToSpeech>(sp => sp.GetRequiredService<AzureTextToSpeech>());
        return services;
    }
}
