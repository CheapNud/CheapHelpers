using System.Text.Json.Serialization;

namespace CheapHelpers.Llm;

public sealed class LlmRequest
{
    /// <summary>Model id. Empty uses <see cref="LlmOptions.Model"/>.</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<LlmMessage> Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LlmToolDefinition>? Tools { get; set; }

    /// <summary>Sampling temperature. Null uses <see cref="LlmOptions.Temperature"/>. The Anthropic client never sends it.</summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    /// <summary>Output token cap. Null uses <see cref="LlmOptions.MaxTokens"/>.</summary>
    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("stream_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamOptions? StreamOptions { get; set; }

    /// <summary>
    /// OpenAI-style effort field. Leave this and <see cref="Reasoning"/> null to let the client
    /// apply <see cref="LlmOptions.ReasoningEffort"/> in the shape the endpoint expects.
    /// </summary>
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }

    /// <summary>OpenRouter's unified reasoning field, the only way it forwards effort to Claude.</summary>
    [JsonPropertyName("reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReasoningOptions? Reasoning { get; set; }

    /// <summary>Spend bucket for <see cref="LlmUsageTracker"/>. Never sent to the API.</summary>
    [JsonIgnore]
    public string Purpose { get; set; } = UsagePurpose.Chat;
}

public sealed class ReasoningOptions
{
    /// <summary>low / medium / high, plus xhigh / max on models that support them.</summary>
    [JsonPropertyName("effort")]
    public string Effort { get; set; } = string.Empty;
}

public sealed class StreamOptions
{
    /// <summary>Ask the provider to emit a final usage chunk on the SSE stream.</summary>
    [JsonPropertyName("include_usage")]
    public bool IncludeUsage { get; set; }
}
