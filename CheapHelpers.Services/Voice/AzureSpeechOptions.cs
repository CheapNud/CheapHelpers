namespace CheapHelpers.Services.Voice;

/// <summary>
/// Configuration shared by the Azure speech-to-text and text-to-speech providers (Azure Speech resource region and key).
/// </summary>
public class AzureSpeechOptions
{
    public const string SectionName = "Voice:Azure";

    /// <summary>Azure region of the Speech resource, e.g. "westeurope".</summary>
    public string Region { get; set; } = string.Empty;
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Language { get; set; } = "en-US";

    /// <summary>Default voice for text-to-speech.</summary>
    public string Voice { get; set; } = "en-US-JennyNeural";

    /// <summary>Value of the X-Microsoft-OutputFormat header for text-to-speech.</summary>
    public string OutputFormat { get; set; } = "audio-24khz-48kbitrate-mono-mp3";
}
