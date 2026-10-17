namespace CheapHelpers.Services.Voice;

/// <summary>
/// Turns recorded audio into text.
/// </summary>
public interface ISpeechToText
{
    /// <summary>
    /// Transcribes <paramref name="audio"/>. Returns an empty Text with Confidence 0 on any failure and logs it;
    /// never throws for network or auth errors. Cancellation of <paramref name="cancellationToken"/> still propagates.
    /// </summary>
    Task<SttResult> RecognizeAsync(ReadOnlyMemory<byte> audio, SttOptions? options = null, CancellationToken cancellationToken = default);
}
