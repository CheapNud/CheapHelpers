namespace CheapHelpers.Services.Voice;

/// <summary>
/// Configuration for <see cref="WhisperSpeechToText"/>. Point <see cref="BaseUrl"/> at a local whisper.cpp server to run offline.
/// </summary>
public class WhisperSttOptions
{
    public const string SectionName = "Voice:Whisper";

    /// <summary>Base URL of an OpenAI-compatible API; "/audio/transcriptions" is appended.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "whisper-1";
    public string Language { get; set; } = "en";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
