namespace CheapHelpers.Services.Voice;

/// <summary>
/// Per-call overrides for <see cref="ITextToSpeech"/>; null means the provider's configured default.
/// </summary>
public sealed class TtsOptions
{
    /// <summary>Voice short name, e.g. "en-US-AriaNeural".</summary>
    public string? Voice { get; init; }

    /// <summary>SSML xml:lang value, e.g. "en-US".</summary>
    public string? Language { get; init; }
}
