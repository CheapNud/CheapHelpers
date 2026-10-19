using System.Text.Json.Serialization;
using Anthropic.Models.Messages;

namespace CheapHelpers.Llm;

public sealed class LlmMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LlmToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Native Anthropic only: the raw content blocks (thinking, text, tool_use) the model
    /// returned for this assistant turn, replayed verbatim on the next tool round. The
    /// API rejects a tool continuation whose thinking blocks are missing or altered.
    /// Never serialised to the OpenAI-compatible wire.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<ContentBlockParam>? ProviderBlocks { get; set; }

    public static LlmMessage System(string content) => new() { Role = "system", Content = content };
    public static LlmMessage User(string content) => new() { Role = "user", Content = content };
    public static LlmMessage Assistant(string content) => new() { Role = "assistant", Content = content };

    public static LlmMessage AssistantToolCalls(List<LlmToolCall> toolCalls, IReadOnlyList<ContentBlockParam>? providerBlocks = null) => new()
    {
        Role = "assistant",
        ToolCalls = toolCalls,
        ProviderBlocks = providerBlocks,
    };

    public static LlmMessage ToolResult(string toolCallId, string name, string content) => new()
    {
        Role = "tool",
        ToolCallId = toolCallId,
        Name = name,
        Content = content,
    };
}

public sealed class LlmToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public LlmFunctionCall Function { get; set; } = new();
}

public sealed class LlmFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
