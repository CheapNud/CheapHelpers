using Anthropic.Models.Messages;

namespace CheapHelpers.Llm;

/// <summary>
/// One delta from a streaming response, carrying a content fragment, a tool call
/// fragment or the finish reason. The Anthropic client's last chunk also carries the
/// assistant turn's <see cref="ProviderBlocks"/> for the next tool round.
/// </summary>
public sealed record LlmStreamChunk(
    string? ContentDelta,
    IReadOnlyList<LlmToolCallDelta>? ToolCallDeltas,
    string? FinishReason,
    IReadOnlyList<ContentBlockParam>? ProviderBlocks = null);

/// <summary>
/// A single tool call delta. The provider streams tool calls piecewise: the first
/// chunk for a call carries the id and function name, later chunks append
/// <see cref="ArgumentsDelta"/> fragments until the JSON is complete. <see cref="Index"/>
/// ties deltas for the same call together when several calls interleave.
/// </summary>
public sealed record LlmToolCallDelta(
    int Index,
    string? Id,
    string? FunctionName,
    string? ArgumentsDelta);
