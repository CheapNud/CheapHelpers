using System.Text.Json;
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
    /// Effort for this request, overriding <see cref="LlmOptions.ReasoningEffort"/>. The clients
    /// send it in the shape the endpoint expects, whichever of this or <see cref="Reasoning"/> is set.
    /// </summary>
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }

    /// <summary>OpenRouter's reasoning object; its effort wins over <see cref="ReasoningEffort"/>.</summary>
    [JsonPropertyName("reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReasoningOptions? Reasoning { get; set; }

    /// <summary>Spend bucket for <see cref="LlmUsageTracker"/>. Never sent to the API.</summary>
    [JsonIgnore]
    public string Purpose { get; set; } = UsagePurpose.Chat;

    /// <summary>
    /// Asks for a JSON reply matching this schema; the reply lands in <see cref="LlmResponse.StructuredJson"/>.
    /// Streaming does not support it.
    /// </summary>
    [JsonIgnore]
    public LlmJsonSchema? ResponseSchema { get; set; }

    // New properties must be copied here too, or they are lost when the input rules rewrite a request.
    internal LlmRequest WithMessages(IReadOnlyList<LlmMessage> messages) => new()
    {
        Model = Model,
        Messages = [.. messages],
        Tools = Tools,
        Temperature = Temperature,
        MaxTokens = MaxTokens,
        Stream = Stream,
        StreamOptions = StreamOptions,
        ReasoningEffort = ReasoningEffort,
        Reasoning = Reasoning,
        Purpose = Purpose,
        ResponseSchema = ResponseSchema,
    };
}

/// <summary>A named JSON Schema for structured output. The schema root must be an object.</summary>
public sealed record LlmJsonSchema(string Name, JsonElement Schema);

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
