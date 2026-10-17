namespace CheapHelpers.Services.Voice;

/// <summary>
/// Per-call overrides for <see cref="ISpeechToText"/>; null means the provider's configured default.
/// </summary>
public sealed class SttOptions
{
    /// <summary>Spoken language, e.g. "en" for Whisper or "en-US" for Azure.</summary>
    public string? Language { get; init; }

    /// <summary>Model name. Only providers that have a model choice (Whisper) use it.</summary>
    public string? Model { get; init; }
}
