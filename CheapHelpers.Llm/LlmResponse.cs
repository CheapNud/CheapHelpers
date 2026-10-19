using System.Text.Json.Serialization;

namespace CheapHelpers.Llm;

public sealed class LlmResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<LlmChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public LlmUsage? Usage { get; set; }

    public LlmMessage? FirstMessage => Choices.Count > 0 ? Choices[0].Message : null;
    public string? TextContent => FirstMessage?.Content?.ToString();
    public List<LlmToolCall>? ToolCalls => FirstMessage?.ToolCalls;
    public bool HasToolCalls => ToolCalls is { Count: > 0 };
    public string? FinishReason => Choices.Count > 0 ? Choices[0].FinishReason : null;

    /// <summary>The JSON reply when the request carried <see cref="LlmRequest.ResponseSchema"/>; null when the model gave none.</summary>
    [JsonIgnore]
    public string? StructuredJson { get; set; }
}

public sealed class LlmChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public LlmMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class LlmUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>Native Anthropic only: prompt tokens served from the prompt cache (billed at 0.1x).</summary>
    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }

    /// <summary>Native Anthropic only: prompt tokens written to the prompt cache this call.</summary>
    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }
}
