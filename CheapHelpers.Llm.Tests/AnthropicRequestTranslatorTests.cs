using System.Collections.Immutable;
using System.Text.Json;
using Anthropic.Core;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheapHelpers.Llm.Tests;

public sealed class AnthropicRequestTranslatorTests
{
    private static MessageCreateParams Translate(LlmRequest request, string? effort = null) =>
        AnthropicRequestTranslator.ToMessageParams(request, new LlmOptions { Model = "claude-haiku-5-5", ReasoningEffort = effort }, NullLogger.Instance);

    [Fact]
    public void System_message_is_hoisted_with_a_cache_marker()
    {
        var request = new LlmRequest
        {
            Model = "claude-haiku-5-5",
            Messages = [LlmMessage.System("You are a helpful assistant."), LlmMessage.User("hi")],
        };

        var p = Translate(request);

        var systemBlocks = Assert.IsType<ImmutableArray<TextBlockParam>>(p.System!.Value);
        var block = Assert.Single(systemBlocks);
        Assert.Equal("You are a helpful assistant.", block.Text);
        Assert.NotNull(block.CacheControl);
        Assert.Single(p.Messages);
        Assert.Equal<ApiEnum<string, Role>>(Role.User, p.Messages[0].Role);
    }

    [Fact]
    public void Empty_model_and_max_tokens_fall_back_to_the_options()
    {
        var p = AnthropicRequestTranslator.ToMessageParams(
            new LlmRequest { Messages = [LlmMessage.User("hi")] },
            new LlmOptions { Model = "claude-sonnet-5-5", MaxTokens = 1234 },
            NullLogger.Instance);

        Assert.Equal("claude-sonnet-5-5", (string)p.Model);
        Assert.Equal(1234L, p.MaxTokens);
    }

    [Fact]
    public void Tools_map_to_input_schema_and_the_last_one_carries_the_cache_marker()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"target":{"type":"string"}},"required":["target"]}""").RootElement;
        var request = new LlmRequest
        {
            Messages = [LlmMessage.User("hi")],
            Tools =
            [
                LlmToolDefinition.Create("a", "first", null),
                LlmToolDefinition.Create("b", "second", schema),
            ],
        };

        var p = Translate(request);

        Assert.Equal(2, p.Tools!.Count);
        var last = Assert.IsType<Tool>(p.Tools[1].Value);
        Assert.Equal("b", last.Name);
        Assert.Equal("second", last.Description);
        Assert.Contains("target", last.InputSchema.Properties!.Keys);
        Assert.Equal(["target"], last.InputSchema.Required);
        Assert.NotNull(last.CacheControl);
        Assert.Null(Assert.IsType<Tool>(p.Tools[0].Value).CacheControl);
    }

    [Fact]
    public void Input_schema_keeps_every_root_keyword()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"a":{"$ref":"#/$defs/A"}},"$defs":{"A":{"type":"string"}},"additionalProperties":false}""").RootElement;

        var json = JsonSerializer.Serialize(AnthropicRequestTranslator.ToInputSchema(schema));

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("string", doc.RootElement.GetProperty("$defs").GetProperty("A").GetProperty("type").GetString());
        Assert.False(doc.RootElement.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void Tool_calls_and_results_become_tool_use_and_tool_result_blocks()
    {
        var call = new LlmToolCall { Id = "tu_1", Function = new() { Name = "lights", Arguments = """{"on":true}""" } };
        var request = new LlmRequest
        {
            Messages =
            [
                LlmMessage.User("lights on"),
                LlmMessage.AssistantToolCalls([call]),
                LlmMessage.ToolResult("tu_1", "lights", "done"),
            ],
        };

        var p = Translate(request);

        Assert.Equal(3, p.Messages.Count);
        var assistant = Assert.IsType<ImmutableArray<ContentBlockParam>>(p.Messages[1].Content.Value);
        var toolUse = Assert.IsType<ToolUseBlockParam>(Assert.Single(assistant).Value);
        Assert.Equal("tu_1", toolUse.ID);
        Assert.Equal("lights", toolUse.Name);
        var results = Assert.IsType<ImmutableArray<ContentBlockParam>>(p.Messages[2].Content.Value);
        var result = Assert.IsType<ToolResultBlockParam>(Assert.Single(results).Value);
        Assert.Equal("tu_1", result.ToolUseID);
        Assert.Equal<ApiEnum<string, Role>>(Role.User, p.Messages[2].Role);
    }

    [Fact]
    public void Two_tool_results_merge_into_one_user_turn()
    {
        var request = new LlmRequest
        {
            Messages =
            [
                LlmMessage.User("go"),
                LlmMessage.AssistantToolCalls(
                [
                    new() { Id = "a", Function = new() { Name = "x", Arguments = "{}" } },
                    new() { Id = "b", Function = new() { Name = "y", Arguments = "{}" } },
                ]),
                LlmMessage.ToolResult("a", "x", "1"),
                LlmMessage.ToolResult("b", "y", "2"),
            ],
        };

        var p = Translate(request);

        Assert.Equal(3, p.Messages.Count);
        var results = Assert.IsType<ImmutableArray<ContentBlockParam>>(p.Messages[2].Content.Value);
        Assert.Equal(2, results.Length);
    }

    [Fact]
    public void Provider_blocks_replace_the_reconstructed_assistant_turn()
    {
        var blocks = new List<ContentBlockParam>
        {
            new ThinkingBlockParam { Thinking = "", Signature = "sig" },
            new ToolUseBlockParam { ID = "tu_1", Name = "lights", Input = new Dictionary<string, JsonElement>() },
        };
        var call = new LlmToolCall { Id = "tu_1", Function = new() { Name = "lights", Arguments = "{}" } };
        var request = new LlmRequest
        {
            Messages =
            [
                LlmMessage.User("lights on"),
                LlmMessage.AssistantToolCalls([call], blocks),
                LlmMessage.ToolResult("tu_1", "lights", "done"),
            ],
        };

        var p = Translate(request);

        var assistant = Assert.IsType<ImmutableArray<ContentBlockParam>>(p.Messages[1].Content.Value);
        Assert.Equal(2, assistant.Length);
        Assert.IsType<ThinkingBlockParam>(assistant[0].Value);
        Assert.IsType<ToolUseBlockParam>(assistant[1].Value);
    }

    [Fact]
    public void Effort_lands_in_output_config_and_temperature_is_never_sent()
    {
        var request = new LlmRequest { Messages = [LlmMessage.User("hi")], Temperature = 0.3 };

        var p = Translate(request, "high");

        Assert.Equal(Effort.High, p.OutputConfig!.Effort!.Value());
#pragma warning disable CS0618 // asserting the deprecated field stays unset
        Assert.Null(p.Temperature);
#pragma warning restore CS0618
    }

    [Fact]
    public void Request_effort_wins_over_the_options_effort()
    {
        var request = new LlmRequest { Messages = [LlmMessage.User("hi")], ReasoningEffort = "low" };

        var p = Translate(request, "high");

        Assert.Equal(Effort.Low, p.OutputConfig!.Effort!.Value());
    }

    [Fact]
    public void Xhigh_effort_maps_to_the_xhigh_member()
    {
        var p = Translate(new LlmRequest { Messages = [LlmMessage.User("hi")] }, "xhigh");

        Assert.Equal(Effort.Xhigh, p.OutputConfig!.Effort!.Value());
    }

    [Fact]
    public void Window_starting_with_an_assistant_turn_gets_a_leading_user_turn()
    {
        var request = new LlmRequest { Messages = [LlmMessage.System("s"), LlmMessage.Assistant("hi"), LlmMessage.User("x")] };

        var p = Translate(request);

        Assert.Equal<ApiEnum<string, Role>>(Role.User, p.Messages[0].Role);
        Assert.Equal(3, p.Messages.Count);
        var blocks = Assert.IsType<ImmutableArray<ContentBlockParam>>(p.Messages[0].Content.Value);
        Assert.Equal(AnthropicRequestTranslator.TrimmedConversationPlaceholder, Assert.IsType<TextBlockParam>(Assert.Single(blocks).Value).Text);
    }

    [Fact]
    public void Unknown_effort_is_dropped_not_sent()
    {
        var p = Translate(new LlmRequest { Messages = [LlmMessage.User("hi")] }, "turbo");

        Assert.Null(p.OutputConfig);
    }

    [Fact]
    public void Invalid_tool_arguments_throw_with_the_tool_name()
    {
        var call = new LlmToolCall { Id = "tu_1", Function = new() { Name = "lights", Arguments = """{"on":tru""" } };
        var request = new LlmRequest
        {
            Messages = [LlmMessage.User("x"), LlmMessage.AssistantToolCalls([call])],
        };

        var ex = Assert.Throws<JsonException>(() => Translate(request));
        Assert.Contains("lights", ex.Message);
    }

    [Fact]
    public void Response_schema_adds_a_last_tool_with_the_schema_name()
    {
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"score":{"type":"integer"}},"required":["score"]}""").RootElement;
        var request = new LlmRequest
        {
            Messages = [LlmMessage.User("rate it")],
            Tools = [LlmToolDefinition.Create("lookup", "find", null)],
            ResponseSchema = new LlmJsonSchema("Rating", schema),
        };

        var p = Translate(request);

        Assert.Equal(2, p.Tools!.Count);
        var schemaTool = Assert.IsType<Tool>(p.Tools[1].Value);
        Assert.Equal("Rating", schemaTool.Name);
        Assert.Equal(["score"], schemaTool.InputSchema.Required);
        Assert.NotNull(schemaTool.CacheControl);
        Assert.NotNull(p.ToolChoice);
    }
}
