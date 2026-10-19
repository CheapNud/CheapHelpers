using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace CheapHelpers.Llm;

public static partial class LlmClientExtensions
{
    private const string PropertiesKeyword = "properties";
    private const string RequiredKeyword = "required";
    private const string AdditionalPropertiesKeyword = "additionalProperties";
    private const int MaxSchemaNameLength = 64;

    private static readonly JsonSchemaExporterOptions SchemaOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
        // Strict structured output (OpenAI and most OpenAI-compatible providers) rejects an
        // object schema unless every property is required and extra properties are closed off.
        // Nullable properties stay nullable through their type, so this does not force values.
        TransformSchemaNode = (_, schema) =>
        {
            if (schema is JsonObject node && node[PropertiesKeyword] is JsonObject properties)
            {
                node[RequiredKeyword] = new JsonArray([.. properties.Select(property => (JsonNode?)property.Key)]);
                node[AdditionalPropertiesKeyword] = false;
            }
            return schema;
        },
    };

    /// <summary>
    /// Asks for a JSON reply shaped like <typeparamref name="T"/> and deserialises it. Sets
    /// <see cref="LlmRequest.ResponseSchema"/> on <paramref name="request"/>. <typeparamref name="T"/>
    /// must serialise as a JSON object, such as a class or record.
    /// </summary>
    /// <exception cref="InvalidOperationException">The model returned no structured reply.</exception>
    public static async Task<T> GenerateAsync<T>(
        this ILlmClient client,
        LlmRequest request,
        JsonSerializerOptions? jsonOptions = null,
        CancellationToken cancellationToken = default)
    {
        var serializerOptions = jsonOptions ?? JsonSerializerOptions.Web;
        // The exporter needs a type resolver; caller options built without one get the default.
        var schemaOptions = serializerOptions.TypeInfoResolver is null
            ? new JsonSerializerOptions(serializerOptions) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() }
            : serializerOptions;

        var schemaNode = schemaOptions.GetJsonSchemaAsNode(typeof(T), SchemaOptions);
        request.ResponseSchema = new LlmJsonSchema(SchemaName(typeof(T)), JsonSerializer.SerializeToElement(schemaNode));

        var response = await client.GenerateAsync(request, cancellationToken);
        if (response.StructuredJson is null)
            throw new InvalidOperationException($"The model returned no structured reply (finish reason: {response.FinishReason ?? "none"}).");

        return JsonSerializer.Deserialize<T>(response.StructuredJson, schemaOptions)
            ?? throw new InvalidOperationException($"The structured reply deserialised to null (finish reason: {response.FinishReason ?? "none"}).");
    }

    /// <summary>
    /// Schema and tool names allow only letters, digits, '_' and '-' up to 64 characters, so a
    /// generic name such as Result`1 becomes Result_1.
    /// </summary>
    internal static string SchemaName(Type type)
    {
        var name = InvalidNameCharacters().Replace(type.Name, "_");
        return name.Length > MaxSchemaNameLength ? name[..MaxSchemaNameLength] : name;
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex InvalidNameCharacters();
}
