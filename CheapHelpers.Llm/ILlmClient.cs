namespace CheapHelpers.Llm;

/// <summary>
/// Chat completion client. Both calls take the same <see cref="LlmRequest"/>, so callers do not
/// need to know which provider serves it.
/// </summary>
public interface ILlmClient
{
    /// <summary>Sends the request and returns the whole reply.</summary>
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the reply as it is generated. Each chunk carries a text fragment, a tool call
    /// fragment or the finish reason.
    /// </summary>
    IAsyncEnumerable<LlmStreamChunk> GenerateStreamAsync(LlmRequest request, CancellationToken cancellationToken = default);
}
