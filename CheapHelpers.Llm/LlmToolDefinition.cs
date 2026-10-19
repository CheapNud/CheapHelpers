using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Llm;

public sealed class LlmToolDefinition
{
    private const string EmptyObjectSchema = """{"type":"object","properties":{}}""";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public LlmFunctionDefinition Function { get; set; } = new();

    /// <summary>Builds a function tool. A null schema means the tool takes no arguments.</summary>
    public static LlmToolDefinition Create(string name, string description, JsonElement? parametersSchema) => new()
    {
        Function = new LlmFunctionDefinition
        {
            Name = name,
            Description = description,
            Parameters = parametersSchema ?? JsonDocument.Parse(EmptyObjectSchema).RootElement,
        },
    };
}

public sealed class LlmFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; set; }
}
