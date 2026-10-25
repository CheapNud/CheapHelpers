using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm;

/// <summary>
/// Chat client on Anthropic's native Messages API. Same contract as
/// <see cref="OpenAiCompatibleLlmClient"/>, plus prompt caching on the static prefix,
/// first-class effort, and thinking blocks replayed across tool rounds through
/// <see cref="LlmMessage.ProviderBlocks"/>.
/// </summary>
public sealed class AnthropicLlmClient(
    AnthropicClientFactory factory,
    IOptionsMonitor<LlmOptions> options,
    LlmUsageTracker usageTracker,
    ILogger<AnthropicLlmClient> logger) : ILlmClient
{
    private const string EmptyToolInput = "{}";

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = options.CurrentValue;
        if (usageTracker.IsOverBudget(config.MaxSessionTokens))
        {
            logger.LogWarning("Session token budget {MaxSessionTokens} reached, skipping call (purpose={Purpose})", config.MaxSessionTokens, request.Purpose);
            return new LlmResponse();
        }
        request = LlmText.ApplyInputRules(request, config);

        var parameters = AnthropicRequestTranslator.ToMessageParams(request, config, logger);
        logger.LogDebug("Anthropic messages | model={Model} | messages={Messages} | tools={Tools}", parameters.Model, parameters.Messages.Count, parameters.Tools?.Count ?? 0);

        var message = await factory.Get().Messages.Create(parameters, cancellationToken);
        var response = ToLlmResponse(message);
        if (request.ResponseSchema is { } schema)
        {
            // The translator forced a tool named after the schema; its input is the structured reply.
            response.StructuredJson = response.ToolCalls?
                .FirstOrDefault(call => call.Function.Name == schema.Name)?
                .Function.Arguments;
        }
        usageTracker.Record(request.Purpose, response.Usage);
        return response;
    }

    public async IAsyncEnumerable<LlmStreamChunk> GenerateStreamAsync(
        LlmRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.ResponseSchema is not null)
            throw new NotSupportedException(OpenAiCompatibleLlmClient.StreamingSchemaMessage);

        var config = options.CurrentValue;
        if (usageTracker.IsOverBudget(config.MaxSessionTokens))
        {
            logger.LogWarning("Session token budget {MaxSessionTokens} reached, skipping stream (purpose={Purpose})", config.MaxSessionTokens, request.Purpose);
            yield break;
        }
        request = LlmText.ApplyInputRules(request, config);

        var parameters = AnthropicRequestTranslator.ToMessageParams(request, config, logger);
        logger.LogDebug("Anthropic messages (stream) | model={Model} | messages={Messages}", parameters.Model, parameters.Messages.Count);

        var assembler = new BlockAssembler();
        var usage = new LlmUsage();
        string? finishReason = null;

        try
        {
            await foreach (var streamEvent in factory.Get().Messages.CreateStreaming(parameters, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                if (streamEvent.TryPickStart(out var start))
                {
                    var startUsage = ToUsage(start.Message.Usage);
                    usage.PromptTokens = startUsage.PromptTokens;
                    usage.CacheReadInputTokens = startUsage.CacheReadInputTokens;
                    usage.CacheCreationInputTokens = startUsage.CacheCreationInputTokens;
                    continue;
                }

                if (streamEvent.TryPickContentBlockStart(out var blockStart))
                {
                    var index = (int)blockStart.Index;
                    if (blockStart.ContentBlock.TryPickToolUse(out ToolUseBlock? toolUse))
                    {
                        assembler.StartToolUse(index, toolUse.ID, toolUse.Name);
                        yield return new LlmStreamChunk(null, [new LlmToolCallDelta(index, toolUse.ID, toolUse.Name, null)], null);
                    }
                    else if (blockStart.ContentBlock.TryPickThinking(out _))
                    {
                        assembler.StartThinking(index);
                    }
                    else if (blockStart.ContentBlock.TryPickRedactedThinking(out RedactedThinkingBlock? redacted))
                    {
                        assembler.AddRedacted(index, redacted.Data);
                    }
                    else if (blockStart.ContentBlock.TryPickText(out _))
                    {
                        assembler.StartText(index);
                    }
                    continue;
                }

                if (streamEvent.TryPickContentBlockDelta(out var blockDelta))
                {
                    var index = (int)blockDelta.Index;
                    if (blockDelta.Delta.TryPickText(out var textDelta))
                    {
                        assembler.AppendText(index, textDelta.Text);
                        yield return new LlmStreamChunk(textDelta.Text, null, null);
                    }
                    else if (blockDelta.Delta.TryPickInputJson(out var jsonDelta))
                    {
                        assembler.AppendToolJson(index, jsonDelta.PartialJson);
                        yield return new LlmStreamChunk(null, [new LlmToolCallDelta(index, null, null, jsonDelta.PartialJson)], null);
                    }
                    else if (blockDelta.Delta.TryPickThinking(out var thinkingDelta))
                    {
                        assembler.AppendThinking(index, thinkingDelta.Thinking);
                    }
                    else if (blockDelta.Delta.TryPickSignature(out var signatureDelta))
                    {
                        assembler.SetSignature(index, signatureDelta.Signature);
                    }
                    continue;
                }

                if (streamEvent.TryPickDelta(out var messageDelta))
                {
                    finishReason = MapStopReason(messageDelta.Delta.StopReason is { } stopReason ? (string)stopReason : null);
                    usage.CompletionTokens = (int)messageDelta.Usage.OutputTokens;
                    continue;
                }

                if (streamEvent.TryPickStop(out _))
                    break;
            }
        }
        finally
        {
            // message_start already delivered the prompt and cache counts, so record even when the stream faults or is cancelled.
            if (usage.PromptTokens > 0 || usage.CompletionTokens > 0)
            {
                usage.TotalTokens = usage.PromptTokens + usage.CompletionTokens;
                usageTracker.Record(request.Purpose, usage);
            }
        }

        yield return new LlmStreamChunk(null, null, finishReason, assembler.Build(logger));
    }

    /// <summary>
    /// Rebuilds the assistant turn's content blocks from stream events so the next tool
    /// round can replay them verbatim (thinking signature included).
    /// </summary>
    private sealed class BlockAssembler
    {
        private enum SlotKind { Text, Thinking, ToolUse, Redacted }

        private readonly SortedDictionary<int, Slot> _slots = [];

        public void StartText(int index) => _slots[index] = new Slot { Kind = SlotKind.Text };
        public void StartThinking(int index) => _slots[index] = new Slot { Kind = SlotKind.Thinking };
        public void StartToolUse(int index, string id, string name) => _slots[index] = new Slot { Kind = SlotKind.ToolUse, Id = id, Name = name };
        public void AddRedacted(int index, string data) => _slots[index] = new Slot { Kind = SlotKind.Redacted, Data = data };
        public void AppendText(int index, string text) => Get(index).Text.Append(text);
        public void AppendThinking(int index, string text) => Get(index).Text.Append(text);
        public void AppendToolJson(int index, string json) => Get(index).Text.Append(json);
        public void SetSignature(int index, string signature) => Get(index).Signature = signature;

        private Slot Get(int index) => _slots.TryGetValue(index, out var slot) ? slot : _slots[index] = new Slot { Kind = SlotKind.Text };

        public IReadOnlyList<ContentBlockParam> Build(ILogger logger)
        {
            var blocks = new List<ContentBlockParam>();
            foreach (var slot in _slots.Values)
            {
                switch (slot.Kind)
                {
                    case SlotKind.Text:
                        if (slot.Text.Length > 0) blocks.Add(new TextBlockParam { Text = slot.Text.ToString() });
                        break;
                    case SlotKind.Thinking:
                        blocks.Add(new ThinkingBlockParam { Thinking = slot.Text.ToString(), Signature = slot.Signature ?? string.Empty });
                        break;
                    case SlotKind.Redacted:
                        blocks.Add(new RedactedThinkingBlockParam { Data = slot.Data ?? string.Empty });
                        break;
                    case SlotKind.ToolUse:
                        var json = slot.Text.Length == 0 ? EmptyToolInput : slot.Text.ToString();
                        Dictionary<string, JsonElement> input;
                        try
                        {
                            input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
                        }
                        catch (JsonException)
                        {
                            // Stream cut off mid-argument (max_tokens). The API still wants the tool_use echoed back.
                            logger.LogWarning("Tool call '{Tool}' input was cut off (incomplete JSON); replaying it with empty input", slot.Name);
                            input = [];
                        }
                        blocks.Add(new ToolUseBlockParam { ID = slot.Id ?? string.Empty, Name = slot.Name ?? string.Empty, Input = input });
                        break;
                }
            }
            return blocks;
        }

        private sealed class Slot
        {
            public SlotKind Kind;
            public string? Id;
            public string? Name;
            public string? Data;
            public string? Signature;
            public readonly StringBuilder Text = new();
        }
    }

    internal static LlmResponse ToLlmResponse(Message message)
    {
        var text = new StringBuilder();
        var toolCalls = new List<LlmToolCall>();
        var providerBlocks = new List<ContentBlockParam>();

        foreach (var block in message.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock))
            {
                text.Append(textBlock.Text);
                providerBlocks.Add(new TextBlockParam { Text = textBlock.Text });
            }
            else if (block.TryPickThinking(out ThinkingBlock? thinking))
            {
                providerBlocks.Add(new ThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature });
            }
            else if (block.TryPickRedactedThinking(out RedactedThinkingBlock? redacted))
            {
                providerBlocks.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
            }
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
            {
                toolCalls.Add(new LlmToolCall
                {
                    Id = toolUse.ID,
                    Function = new LlmFunctionCall
                    {
                        Name = toolUse.Name,
                        Arguments = JsonSerializer.Serialize(toolUse.Input),
                    },
                });
                providerBlocks.Add(new ToolUseBlockParam { ID = toolUse.ID, Name = toolUse.Name, Input = toolUse.Input });
            }
        }

        var assistant = new LlmMessage
        {
            Role = "assistant",
            Content = text.Length == 0 ? null : text.ToString(),
            ToolCalls = toolCalls.Count == 0 ? null : toolCalls,
            ProviderBlocks = providerBlocks,
        };

        return new LlmResponse
        {
            Id = message.ID,
            Choices = [new LlmChoice { Index = 0, Message = assistant, FinishReason = MapStopReason(message.StopReason is { } stopReason ? (string)stopReason : null) }],
            Usage = ToUsage(message.Usage),
        };
    }

    internal static LlmUsage ToUsage(Usage usage) => new()
    {
        PromptTokens = (int)usage.InputTokens,
        CompletionTokens = (int)usage.OutputTokens,
        TotalTokens = (int)(usage.InputTokens + usage.OutputTokens),
        CacheReadInputTokens = (int)(usage.CacheReadInputTokens ?? 0),
        CacheCreationInputTokens = (int)(usage.CacheCreationInputTokens ?? 0),
    };

    /// <summary>Anthropic stop reasons in the OpenAI finish_reason vocabulary.</summary>
    internal static string? MapStopReason(string? stopReason) => stopReason switch
    {
        null => null,
        "tool_use" => "tool_calls",
        "max_tokens" => "length",
        "refusal" => "refusal",
        _ => "stop",
    };
}
