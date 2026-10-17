namespace CheapHelpers.Services.Voice;

/// <summary>
/// Turns text into audio.
/// </summary>
public interface ITextToSpeech
{
    /// <summary>
    /// Synthesises <paramref name="text"/> into audio in the provider's output format. Returns empty audio on any failure and logs it;
    /// never throws for network or auth errors. Cancellation of <paramref name="cancellationToken"/> still propagates.
    /// </summary>
    Task<ReadOnlyMemory<byte>> SynthesizeAsync(string text, TtsOptions? options = null, CancellationToken cancellationToken = default);
}
