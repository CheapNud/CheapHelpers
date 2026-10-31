using System.Net;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace CheapHelpers.Llm.Tests;

public sealed class AnthropicLlmClientTests
{
    private const string ToolUseReply = """
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[
           {"type":"thinking","thinking":"","signature":"sig123"},
           {"type":"text","text":"Lights coming on."},
           {"type":"tool_use","id":"tu_1","name":"lights","input":{"on":true}}
         ],
         "stop_reason":"tool_use","stop_sequence":null,
         "usage":{"input_tokens":120,"output_tokens":30,"cache_read_input_tokens":2000,"cache_creation_input_tokens":0}}
        """;

    private const string SchemaToolReply = """
        {"id":"msg_9","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"tool_use","id":"tu_9","name":"Rating","input":{"score":4,"reason":"solid"}}],
         "stop_reason":"tool_use","stop_sequence":null,
         "usage":{"input_tokens":50,"output_tokens":20}}
        """;

    private const string LocalUrl = "http://localhost/";

    private static LlmOptions Options(long maxSessionTokens = 0) => new()
    {
        Provider = LlmProvider.Anthropic,
        ApiKey = "k",
        BaseUrl = "https://api.anthropic.com/v1",
        Model = "claude-haiku-5-5",
        MaxSessionTokens = maxSessionTokens,
    };

    private static (AnthropicLlmClient Client, RecordingHttpHandler Server, LlmUsageTracker Tracker) Make(LlmOptions? options = null)
    {
        var server = new RecordingHttpHandler();
        var tracker = new LlmUsageTracker();
        return (TestClients.Anthropic(server, options ?? Options(), tracker), server, tracker);
    }

    [Fact]
    public async Task Generate_maps_text_tool_calls_usage_and_provider_blocks()
    {
        var (client, server, tracker) = Make();
        server.Reply(ToolUseReply);

        var response = await client.GenerateAsync(new LlmRequest
        {
            Model = "claude-haiku-5-5",
            Messages = [LlmMessage.System("sys"), LlmMessage.User("lights on")],
            Purpose = UsagePurpose.Chat,
        }, TestContext.Current.CancellationToken);

        Assert.Equal("Lights coming on.", response.TextContent);
        var call = Assert.Single(response.ToolCalls!);
        Assert.Equal("tu_1", call.Id);
        Assert.Equal("lights", call.Function.Name);
        Assert.True(JsonDocument.Parse(call.Function.Arguments).RootElement.GetProperty("on").GetBoolean());
        Assert.Equal("tool_calls", response.FinishReason);
        Assert.Equal(3, response.FirstMessage!.ProviderBlocks!.Count);
        Assert.Null(response.StructuredJson);

        Assert.Equal(120, response.Usage!.PromptTokens);
        Assert.Equal(30, response.Usage.CompletionTokens);
        Assert.Equal(2000, response.Usage.CacheReadInputTokens);
        Assert.Equal(120 + 30 + 200, tracker.TotalTokens);
    }

    [Fact]
    public async Task Generate_posts_a_messages_api_body_without_temperature()
    {
        var (client, server, _) = Make();
        server.Reply(ToolUseReply);

        await client.GenerateAsync(new LlmRequest
        {
            Messages = [LlmMessage.System("sys"), LlmMessage.User("hi"), LlmMessage.Assistant("ok"), LlmMessage.User("again")],
            Temperature = 0.5,
        }, TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(server.RequestBodies)!);
        var root = body.RootElement;
        Assert.Equal("claude-haiku-5-5", root.GetProperty("model").GetString());
        Assert.False(root.TryGetProperty("temperature", out _));
        Assert.Equal("ephemeral", root.GetProperty("system")[0].GetProperty("cache_control").GetProperty("type").GetString());
        Assert.Equal("user", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("assistant", root.GetProperty("messages")[1].GetProperty("role").GetString());
    }

    [Fact]
    public async Task Generate_with_a_schema_forces_the_schema_tool_and_fills_structured_json()
    {
        var (client, server, _) = Make();
        server.Reply(SchemaToolReply);
        var schema = JsonDocument.Parse("""{"type":"object","properties":{"score":{"type":"integer"},"reason":{"type":"string"}},"required":["score","reason"]}""").RootElement;

        var response = await client.GenerateAsync(new LlmRequest
        {
            Messages = [LlmMessage.User("rate it")],
            ResponseSchema = new LlmJsonSchema("Rating", schema),
        }, TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(server.RequestBodies)!);
        var toolChoice = body.RootElement.GetProperty("tool_choice");
        Assert.Equal("tool", toolChoice.GetProperty("type").GetString());
        Assert.Equal("Rating", toolChoice.GetProperty("name").GetString());
        var tool = Assert.Single(body.RootElement.GetProperty("tools").EnumerateArray());
        Assert.Equal("Rating", tool.GetProperty("name").GetString());
        Assert.Equal("integer", tool.GetProperty("input_schema").GetProperty("properties").GetProperty("score").GetProperty("type").GetString());

        using var structured = JsonDocument.Parse(response.StructuredJson!);
        Assert.Equal(4, structured.RootElement.GetProperty("score").GetInt32());
        Assert.Equal("solid", structured.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Generate_skips_the_call_when_over_budget()
    {
        var (client, server, tracker) = Make(Options(maxSessionTokens: 10));
        tracker.Record(UsagePurpose.Chat, new LlmUsage { PromptTokens = 10, CompletionTokens = 5 });

        var response = await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.Empty(server.RequestBodies);
        Assert.Null(response.TextContent);
    }

    [Fact]
    public async Task Generate_strips_zero_width_spaces_from_user_text_when_enabled()
    {
        var options = Options();
        options.StripInvisibleCharacters = true;
        var (client, server, _) = Make(options);
        server.Reply(ToolUseReply);

        await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hel​lo​ world")] }, TestContext.Current.CancellationToken);

        var body = Assert.Single(server.RequestBodies);
        // Ordinal: a culture-aware search ignores zero-width characters and would match anywhere.
        Assert.DoesNotContain("​", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u200B", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hello world", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_with_a_cancelled_token_throws_before_sending()
    {
        var (client, server, _) = Make();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, cts.Token));

        Assert.Empty(server.Requests);
    }

    [Fact]
    public void Stop_reasons_map_to_the_openai_vocabulary()
    {
        Assert.Equal("stop", AnthropicLlmClient.MapStopReason("end_turn"));
        Assert.Equal("tool_calls", AnthropicLlmClient.MapStopReason("tool_use"));
        Assert.Equal("length", AnthropicLlmClient.MapStopReason("max_tokens"));
        Assert.Equal("refusal", AnthropicLlmClient.MapStopReason("refusal"));
        Assert.Equal("stop", AnthropicLlmClient.MapStopReason("stop_sequence"));
        Assert.Null(AnthropicLlmClient.MapStopReason(null));
    }

    private const string ToolUseStream =
        "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_1\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-haiku-5-5\",\"content\":[],\"stop_reason\":null,\"stop_sequence\":null,\"usage\":{\"input_tokens\":120,\"output_tokens\":1,\"cache_read_input_tokens\":2000,\"cache_creation_input_tokens\":0}}}\n\n" +
        "event: content_block_start\ndata: {\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"thinking\",\"thinking\":\"\",\"signature\":\"\"}}\n\n" +
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"signature_delta\",\"signature\":\"sig123\"}}\n\n" +
        "event: content_block_stop\ndata: {\"type\":\"content_block_stop\",\"index\":0}\n\n" +
        "event: content_block_start\ndata: {\"type\":\"content_block_start\",\"index\":1,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}\n\n" +
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":1,\"delta\":{\"type\":\"text_delta\",\"text\":\"Lights \"}}\n\n" +
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":1,\"delta\":{\"type\":\"text_delta\",\"text\":\"coming on.\"}}\n\n" +
        "event: content_block_stop\ndata: {\"type\":\"content_block_stop\",\"index\":1}\n\n" +
        "event: content_block_start\ndata: {\"type\":\"content_block_start\",\"index\":2,\"content_block\":{\"type\":\"tool_use\",\"id\":\"tu_1\",\"name\":\"lights\",\"input\":{}}}\n\n" +
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":2,\"delta\":{\"type\":\"input_json_delta\",\"partial_json\":\"{\\\"on\\\":\"}}\n\n" +
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":2,\"delta\":{\"type\":\"input_json_delta\",\"partial_json\":\"true}\"}}\n\n" +
        "event: content_block_stop\ndata: {\"type\":\"content_block_stop\",\"index\":2}\n\n" +
        "event: message_delta\ndata: {\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"tool_use\",\"stop_sequence\":null},\"usage\":{\"output_tokens\":30}}\n\n" +
        "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";

    private const string RefusalStream =
        "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_2\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-haiku-5-5\",\"content\":[],\"stop_reason\":null,\"stop_sequence\":null,\"usage\":{\"input_tokens\":10,\"output_tokens\":1}}}\n\n" +
        "event: message_delta\ndata: {\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"refusal\",\"stop_sequence\":null},\"usage\":{\"output_tokens\":1}}\n\n" +
        "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";

    [Fact]
    public async Task Stream_yields_text_deltas_tool_deltas_and_a_final_chunk_with_provider_blocks()
    {
        var (client, server, tracker) = Make();
        server.Reply(ToolUseStream, contentType: "text/event-stream");

        var chunks = new List<LlmStreamChunk>();
        await foreach (var chunk in client.GenerateStreamAsync(new LlmRequest { Messages = [LlmMessage.User("lights on")] }, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Equal("Lights coming on.", string.Concat(chunks.Select(c => c.ContentDelta)));

        var toolDeltas = chunks.Where(c => c.ToolCallDeltas is not null).SelectMany(c => c.ToolCallDeltas!).ToList();
        Assert.Equal("tu_1", toolDeltas[0].Id);
        Assert.Equal("lights", toolDeltas[0].FunctionName);
        Assert.Equal(2, toolDeltas[0].Index);
        Assert.Equal("""{"on":true}""", string.Concat(toolDeltas.Select(d => d.ArgumentsDelta)));

        var last = chunks[^1];
        Assert.Equal("tool_calls", last.FinishReason);
        Assert.NotNull(last.ProviderBlocks);
        Assert.Equal(3, last.ProviderBlocks!.Count);
        Assert.IsType<ThinkingBlockParam>(last.ProviderBlocks[0].Value);
        Assert.IsType<ToolUseBlockParam>(last.ProviderBlocks[2].Value);

        Assert.Equal(120 + 30 + 200, tracker.TotalTokens);
    }

    [Fact]
    public async Task Stream_refusal_ends_with_the_refusal_finish_reason_and_no_exception()
    {
        var (client, server, _) = Make();
        server.Reply(RefusalStream, contentType: "text/event-stream");

        var chunks = new List<LlmStreamChunk>();
        await foreach (var chunk in client.GenerateStreamAsync(new LlmRequest { Messages = [LlmMessage.User("x")] }, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Equal("refusal", chunks[^1].FinishReason);
        Assert.All(chunks, c => Assert.True(string.IsNullOrEmpty(c.ContentDelta)));
    }

    [Fact]
    public async Task Stream_posts_stream_true_and_the_cache_marked_tools()
    {
        var (client, server, _) = Make();
        server.Reply(RefusalStream, contentType: "text/event-stream");

        await foreach (var _ in client.GenerateStreamAsync(new LlmRequest
        {
            Messages = [LlmMessage.User("x")],
            Tools = [LlmToolDefinition.Create("lights", "toggle", null)],
        }, TestContext.Current.CancellationToken)) { }

        using var body = JsonDocument.Parse(Assert.Single(server.RequestBodies)!);
        Assert.True(body.RootElement.GetProperty("stream").GetBoolean());
        var tools = body.RootElement.GetProperty("tools");
        Assert.Equal("ephemeral", tools[tools.GetArrayLength() - 1].GetProperty("cache_control").GetProperty("type").GetString());
        Assert.Equal("object", tools[0].GetProperty("input_schema").GetProperty("type").GetString());
    }

    [Fact]
    public async Task Stream_with_a_schema_is_not_supported()
    {
        var (client, server, _) = Make();
        var schema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement;

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in client.GenerateStreamAsync(new LlmRequest
            {
                Messages = [LlmMessage.User("x")],
                ResponseSchema = new LlmJsonSchema("Empty", schema),
            }, TestContext.Current.CancellationToken)) { }
        });

        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task Stream_that_fails_mid_way_still_records_the_prompt_tokens()
    {
        var (client, server, tracker) = Make();
        var startOnly = ToolUseStream[..ToolUseStream.IndexOf("event: content_block_start", StringComparison.Ordinal)];
        server.Reply(startOnly + "event: content_block_delta\ndata: {not json\n\n", contentType: "text/event-stream");

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await foreach (var _ in client.GenerateStreamAsync(new LlmRequest { Messages = [LlmMessage.User("x")] }, TestContext.Current.CancellationToken)) { }
        });

        Assert.Equal(120 + 200, tracker.TotalTokens);
    }

    [Fact]
    public void Factory_rebuilds_the_client_when_timeout_or_retries_change()
    {
        var options = Options();
        var factory = new AnthropicClientFactory(new TestOptionsMonitor<LlmOptions>(options));

        var first = factory.Get();
        Assert.Same(first, factory.Get());

        options.TimeoutSeconds = 90;
        Assert.NotSame(first, factory.Get());
    }

    [Fact]
    public async Task Factory_disposes_the_clients_it_owns_when_replaced_and_when_disposed()
    {
        var options = Options();
        var factory = new AnthropicClientFactory(new TestOptionsMonitor<LlmOptions>(options));

        var first = (AnthropicClient)factory.Get();
        options.TimeoutSeconds = 90;
        var second = (AnthropicClient)factory.Get();
        options.MaxRetries = 5;
        var third = (AnthropicClient)factory.Get();

        Assert.NotSame(first, second);
        Assert.NotSame(second, third);
        Assert.Same(third, factory.Get());
        // A disposed HttpClient throws before it sends anything
        await Assert.ThrowsAsync<ObjectDisposedException>(() => first.HttpClient.GetAsync(LocalUrl, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => second.HttpClient.GetAsync(LocalUrl, TestContext.Current.CancellationToken));

        factory.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => third.HttpClient.GetAsync(LocalUrl, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Factory_never_disposes_an_injected_HttpClient()
    {
        var options = Options();
        var server = new RecordingHttpHandler().Reply("{}");
        using var httpClient = server.CreateClient();
        var factory = new AnthropicClientFactory(new TestOptionsMonitor<LlmOptions>(options), httpClient);

        var first = factory.Get();
        options.TimeoutSeconds = 90;
        Assert.NotSame(first, factory.Get());
        factory.Dispose();

        using var reply = await httpClient.PostAsync(LocalUrl, new StringContent("x"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
    }

    [Fact]
    public void Factory_strips_the_preset_version_suffix_from_the_base_url()
    {
        Assert.Equal("https://api.anthropic.com", AnthropicClientFactory.NormalizeBaseUrl("https://api.anthropic.com/v1/"));
        Assert.Equal("https://api.anthropic.com", AnthropicClientFactory.NormalizeBaseUrl(""));
        Assert.Equal("https://proxy.test/anthropic", AnthropicClientFactory.NormalizeBaseUrl("https://proxy.test/anthropic"));
    }

    private const string CutOffToolStream =
        "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_3\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-haiku-5-5\",\"content\":[],\"stop_reason\":null,\"stop_sequence\":null,\"usage\":{\"input_tokens\":50,\"output_tokens\":1}}}\n\n" +
        "event: content_block_start\ndata: {\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"tool_use\",\"id\":\"tu_1\",\"name\":\"lights\",\"input\":{}}}\n\n" +
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"input_json_delta\",\"partial_json\":\"{\\\"on\\\":\"}}\n\n" +
        "event: content_block_stop\ndata: {\"type\":\"content_block_stop\",\"index\":0}\n\n" +
        "event: message_delta\ndata: {\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"max_tokens\",\"stop_sequence\":null},\"usage\":{\"output_tokens\":8}}\n\n" +
        "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";

    [Fact]
    public async Task Stream_cut_off_inside_a_tool_call_still_ends_with_length_and_no_exception()
    {
        var (client, server, tracker) = Make();
        server.Reply(CutOffToolStream, contentType: "text/event-stream");

        var chunks = new List<LlmStreamChunk>();
        await foreach (var chunk in client.GenerateStreamAsync(new LlmRequest { Messages = [LlmMessage.User("x")] }, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        var last = chunks[^1];
        Assert.Equal("length", last.FinishReason);
        var toolUse = Assert.IsType<ToolUseBlockParam>(Assert.Single(last.ProviderBlocks!).Value);
        Assert.Equal("lights", toolUse.Name);
        Assert.Equal(50 + 8, tracker.TotalTokens);
    }

    private const string WorkspaceHeader = "anthropic-workspace-id";
    private const string WorkspaceIdVariable = "ANTHROPIC_WORKSPACE_ID";
    private const string ProfileVariable = "ANTHROPIC_PROFILE";
    private const string ConfigDirVariable = "ANTHROPIC_CONFIG_DIR";

    [Fact]
    public async Task Generate_sends_no_workspace_header_when_the_workspace_variable_is_set()
    {
        var saved = Environment.GetEnvironmentVariable(WorkspaceIdVariable);
        try
        {
            Environment.SetEnvironmentVariable(WorkspaceIdVariable, "wrkspc_from_machine");
            var (client, server, _) = Make();
            server.Reply(ToolUseReply);

            await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("x")] }, TestContext.Current.CancellationToken);

            Assert.False(Assert.Single(server.Requests).Headers.Contains(WorkspaceHeader));
        }
        finally
        {
            Environment.SetEnvironmentVariable(WorkspaceIdVariable, saved);
        }
    }

    // The SDK copies workspace_id from a local profile into this header, but only when the client
    // was given no credential. It throws for a named profile it cannot find, so a call that goes
    // through shows the profile lookup never ran.
    [Fact]
    public async Task Generate_skips_the_local_profile_and_sends_no_workspace_header()
    {
        var savedProfile = Environment.GetEnvironmentVariable(ProfileVariable);
        var savedConfigDir = Environment.GetEnvironmentVariable(ConfigDirVariable);
        try
        {
            Environment.SetEnvironmentVariable(ProfileVariable, "missing-profile");
            Environment.SetEnvironmentVariable(ConfigDirVariable, Path.Combine(AppContext.BaseDirectory, "no-anthropic-config"));
            var (client, server, _) = Make();
            server.Reply(ToolUseReply);

            await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("x")] }, TestContext.Current.CancellationToken);

            Assert.False(Assert.Single(server.Requests).Headers.Contains(WorkspaceHeader));
        }
        finally
        {
            Environment.SetEnvironmentVariable(ProfileVariable, savedProfile);
            Environment.SetEnvironmentVariable(ConfigDirVariable, savedConfigDir);
        }
    }
}
