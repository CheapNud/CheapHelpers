namespace CheapHelpers.Services.Voice;

/// <summary>
/// The outcome of a speech-to-text call. An empty <paramref name="Text"/> with <paramref name="Confidence"/> 0 means nothing was recognised.
/// </summary>
public sealed record SttResult(string Text, double Confidence, string Language);
