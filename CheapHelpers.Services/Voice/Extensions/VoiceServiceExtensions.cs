using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}
