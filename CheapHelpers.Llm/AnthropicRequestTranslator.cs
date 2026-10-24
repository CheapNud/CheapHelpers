using System.Text.Json;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Llm;

/// <summary>
/// Pure mapping from the OpenAI-shaped <see cref="LlmRequest"/> to Anthropic's Messages API
/// parameters. No I/O. Cache markers go on the system block and the last tool so the whole
/// static prefix is served from the prompt cache between tool rounds.
/// </summary>
internal static class AnthropicRequestTranslator
{
    /// <summary>Leading user turn for a message window that starts on an assistant turn.</summary>
    internal const string TrimmedConversationPlaceholder = "(earlier conversation trimmed)";

    private const string EmptyArguments = "{}";
    private const string SchemaToolDescription = "Return the result. The input must match this schema.";
    private const string TypeKeyword = "type";
    private static readonly JsonElement ObjectType = JsonSerializer.SerializeToElement("object");

    public static MessageCreateParams ToMessageParams(LlmRequest request, LlmOptions options, ILogger logger)
    {
        var systemText = string.Join("\n", request.Messages
            .Where(m => m.Role == "system")
            .Select(m => m.Content?.ToString() ?? string.Empty));

        // Structured output on the Messages API: one extra tool shaped like the schema, forced
        // through tool_choice, so the tool input is the JSON reply.
        List<LlmToolDefinition> toolDefinitions = request.ResponseSchema is { } schema
            ? [.. request.Tools ?? [], LlmToolDefinition.Create(schema.Name, SchemaToolDescription, schema.Schema)]
            : request.Tools ?? [];

        var tools = toolDefinitions.Count > 0
            ? toolDefinitions.Select((t, i) => (ToolUnion)new Tool
            {
                Name = t.Function.Name,
                Description = t.Function.Description,
                InputSchema = ToInputSchema(t.Function.Parameters),
                // Only the last tool carries the marker; it caches the whole tool list.
                CacheControl = i == toolDefinitions.Count - 1 ? new CacheControlEphemeral() : null,
            }).ToList()
            : null;

        var effort = ParseEffort(request.Reasoning?.Effort ?? request.ReasoningEffort ?? options.ReasoningEffort, logger);

        var parameters = new MessageCreateParams
        {
            Model = string.IsNullOrEmpty(request.Model) ? options.Model : request.Model,
            MaxTokens = request.MaxTokens ?? options.MaxTokens,
            Messages = ToMessages(request.Messages),
        };

        // The SDK's optional properties reject an explicit null, so they are only set when present.
        if (!string.IsNullOrEmpty(systemText))
        {
            parameters = parameters with
            {
                System = new List<TextBlockParam> { new() { Text = systemText, CacheControl = new CacheControlEphemeral() } },
            };
        }

        if (tools is not null)
            parameters = parameters with { Tools = tools };

        if (request.ResponseSchema is not null)
            parameters = parameters with { ToolChoice = new ToolChoiceTool { Name = request.ResponseSchema.Name } };

        if (effort is not null)
            parameters = parameters with { OutputConfig = new OutputConfig { Effort = effort.Value } };

        return parameters;
    }

    /// <summary>
    /// Tool schemas are full JSON Schema objects. Every root keyword is copied as is, so
    /// $defs, additionalProperties and anyOf survive; only a missing "type" is filled in,
    /// because the Messages API requires an object schema.
    /// </summary>
    public static InputSchema ToInputSchema(JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object) return new InputSchema();

        var raw = parameters.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        raw.TryAdd(TypeKeyword, ObjectType);
        return InputSchema.FromRawUnchecked(raw);
    }

    private static Effort? ParseEffort(string? reasoningEffort, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(reasoningEffort)) return null;
        switch (reasoningEffort.Trim().ToLowerInvariant())
        {
            case "low": return Effort.Low;
            case "medium": return Effort.Medium;
            case "high": return Effort.High;
            case "xhigh": return Effort.Xhigh;
            case "max": return Effort.Max;
            default:
                logger.LogWarning("Unknown reasoning effort '{Effort}', sending the model default", reasoningEffort);
                return null;
        }
    }

    private static List<MessageParam> ToMessages(IReadOnlyList<LlmMessage> messages)
    {
        var output = new List<MessageParam>();
        MessageParamRole? currentRole = null;
        List<ContentBlockParam>? currentBlocks = null;

        void Append(MessageParamRole role, IEnumerable<ContentBlockParam> blocks)
        {
            // Consecutive same-role turns are merged into one message: the API merges them
            // server-side anyway, and one user turn per tool-result batch is the documented shape.
            if (currentRole != role)
            {
                Flush();
                currentRole = role;
                currentBlocks = [];
            }
            currentBlocks!.AddRange(blocks);
        }

        void Flush()
        {
            if (currentRole is not null && currentBlocks is { Count: > 0 })
            {
                if (currentRole == MessageParamRole.User)
                    output.Add(new UserMessageParam(currentBlocks));
                else
                    output.Add(new AssistantMessageParam(currentBlocks));
            }
            currentRole = null;
            currentBlocks = null;
        }

        foreach (var message in messages)
        {
            switch (message.Role)
            {
                case "system":
                    break;

                case "user":
                    Append(MessageParamRole.User, [new TextBlockParam { Text = message.Content?.ToString() ?? string.Empty }]);
                    break;

                case "assistant":
                    Append(MessageParamRole.Assistant, message.ProviderBlocks ?? ReconstructAssistant(message));
                    break;

                case "tool":
                    Append(MessageParamRole.User,
                    [
                        new ToolResultBlockParam
                        {
                            ToolUseID = message.ToolCallId ?? string.Empty,
                            Content = message.Content?.ToString() ?? string.Empty,
                        },
                    ]);
                    break;
            }
        }

        Flush();

        // The API requires the first message to be a user turn; a trimmed window can start on an assistant one.
        if (output.Count > 0 && output[0].TryPickAssistant(out _))
            output.Insert(0, new UserMessageParam(new List<ContentBlockParam> { new TextBlockParam { Text = TrimmedConversationPlaceholder } }));

        return output;
    }

    private static List<ContentBlockParam> ReconstructAssistant(LlmMessage message)
    {
        var blocks = new List<ContentBlockParam>();
        var text = message.Content?.ToString();
        if (!string.IsNullOrEmpty(text))
            blocks.Add(new TextBlockParam { Text = text });

        foreach (var call in message.ToolCalls ?? [])
        {
            Dictionary<string, JsonElement> input;
            try
            {
                input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    string.IsNullOrWhiteSpace(call.Function.Arguments) ? EmptyArguments : call.Function.Arguments) ?? [];
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Tool call '{call.Function.Name}' has invalid JSON arguments: {ex.Message}", ex);
            }

            blocks.Add(new ToolUseBlockParam { ID = call.Id, Name = call.Function.Name, Input = input });
        }

        return blocks;
    }
}
