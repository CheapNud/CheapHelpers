using System.Net;
using System.Text.Json;

namespace CheapHelpers.Llm.Tests;

public sealed class OpenAiCompatibleLlmClientTests
{
    private static LlmOptions Options(string baseUrl = "https://openrouter.ai/api/v1") => new()
    {
        ApiKey = "k",
        BaseUrl = baseUrl,
        Model = "mistralai/mistral-large-4-0",
        MaxRetries = 2,
    };

    private static readonly JsonElement RatingSchema = JsonDocument.Parse(
        """{"type":"object","properties":{"score":{"type":"integer"}},"required":["score"],"additionalProperties":false}""").RootElement;

    [Fact]
    public async Task Generate_fills_model_temperature_and_max_tokens_from_the_options()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("hi"));
        var client = TestClients.Compatible(server, Options());

        var response = await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.Equal("hi", response.TextContent);
        using var body = JsonDocument.Parse(Assert.Single(server.PostedBodies));
        Assert.Equal("mistralai/mistral-large-4-0", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(1.0, body.RootElement.GetProperty("temperature").GetDouble());
        Assert.Equal(4096, body.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("response_format", out _));
        Assert.Equal("https://openrouter.ai/api/v1/chat/completions", server.Requests[0].RequestUri!.ToString());
        Assert.Equal("Bearer k", server.Requests[0].Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Attribution_headers_are_sent_only_when_configured()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("a")).Reply(TestClients.CompatReply("b"));
        var options = Options();
        var client = TestClients.Compatible(server, options);

        await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);
        options.Referer = "https://app.test";
        options.AppTitle = "Test App";
        await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.False(server.Requests[0].Headers.Contains("HTTP-Referer"));
        Assert.False(server.Requests[0].Headers.Contains("X-Title"));
        Assert.Equal("https://app.test", Assert.Single(server.Requests[1].Headers.GetValues("HTTP-Referer")));
        Assert.Equal("Test App", Assert.Single(server.Requests[1].Headers.GetValues("X-Title")));
    }

    [Fact]
    public async Task Generate_with_a_schema_sends_a_strict_response_format_and_fills_structured_json()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("""{"score":4}"""));
        var client = TestClients.Compatible(server, Options());

        var response = await client.GenerateAsync(new LlmRequest
        {
            Messages = [LlmMessage.User("rate it")],
            ResponseSchema = new LlmJsonSchema("Rating", RatingSchema),
        }, TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(server.PostedBodies));
        var format = body.RootElement.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        var jsonSchema = format.GetProperty("json_schema");
        Assert.Equal("Rating", jsonSchema.GetProperty("name").GetString());
        Assert.True(jsonSchema.GetProperty("strict").GetBoolean());
        Assert.Equal("integer", jsonSchema.GetProperty("schema").GetProperty("properties").GetProperty("score").GetProperty("type").GetString());

        Assert.Equal("""{"score":4}""", response.StructuredJson);
    }

    [Fact]
    public async Task Retries_send_a_fresh_request_with_the_same_body()
    {
        var server = new FakeLlmServer()
            .Reply("""{"error":"busy"}""", HttpStatusCode.ServiceUnavailable)
            .Reply(TestClients.CompatReply("ok"));
        var client = TestClients.Compatible(server, Options());

        var response = await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.Equal("ok", response.TextContent);
        Assert.Equal(2, server.PostedBodies.Count);
        Assert.Equal(server.PostedBodies[0], server.PostedBodies[1]);
        Assert.NotSame(server.Requests[0], server.Requests[1]);
    }

    [Fact]
    public async Task Client_errors_are_not_retried()
    {
        var server = new FakeLlmServer().Reply("""{"error":"bad key"}""", HttpStatusCode.Unauthorized);
        var client = TestClients.Compatible(server, Options());

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken));

        Assert.Contains("401", ex.Message);
        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task Generate_with_a_cancelled_token_throws_before_sending()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("never"));
        var client = TestClients.Compatible(server, Options());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, cts.Token));

        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task Generate_skips_the_call_when_over_budget()
    {
        var tracker = new LlmUsageTracker();
        tracker.Record(UsagePurpose.Chat, new LlmUsage { PromptTokens = 10, CompletionTokens = 5 });
        var server = new FakeLlmServer();
        var options = Options();
        options.MaxSessionTokens = 10;
        var client = TestClients.Compatible(server, options, tracker);

        var response = await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.Empty(server.Requests);
        Assert.Null(response.TextContent);
    }

    [Fact]
    public async Task Stream_yields_content_and_records_the_trailing_usage_chunk()
    {
        const string stream =
            "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hel\"}}]}\n\n" +
            "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"lo\"},\"finish_reason\":\"stop\"}]}\n\n" +
            "data: {\"choices\":[],\"usage\":{\"prompt_tokens\":7,\"completion_tokens\":3,\"total_tokens\":10}}\n\n" +
            "data: [DONE]\n\n";
        var tracker = new LlmUsageTracker();
        var server = new FakeLlmServer().Reply(stream, contentType: "text/event-stream");
        var client = TestClients.Compatible(server, Options(), tracker);

        var chunks = new List<LlmStreamChunk>();
        await foreach (var chunk in client.GenerateStreamAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken))
            chunks.Add(chunk);

        Assert.Equal("Hello", string.Concat(chunks.Select(c => c.ContentDelta)));
        Assert.Equal("stop", chunks[^1].FinishReason);
        Assert.Equal(10, tracker.TotalTokens);
        using var body = JsonDocument.Parse(Assert.Single(server.PostedBodies));
        Assert.True(body.RootElement.GetProperty("stream").GetBoolean());
        Assert.True(body.RootElement.GetProperty("stream_options").GetProperty("include_usage").GetBoolean());
    }

    [Fact]
    public async Task Stream_error_event_after_the_headers_throws()
    {
        const string stream =
            "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hel\"}}]}\n\n" +
            "data: {\"error\":{\"message\":\"upstream failure\"}}\n\n";
        var server = new FakeLlmServer().Reply(stream, contentType: "text/event-stream");
        var client = TestClients.Compatible(server, Options());

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in client.GenerateStreamAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken)) { }
        });

        Assert.Contains("upstream failure", ex.Message);
    }

    [Fact]
    public async Task Stream_with_a_schema_is_not_supported()
    {
        var server = new FakeLlmServer();
        var client = TestClients.Compatible(server, Options());

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in client.GenerateStreamAsync(new LlmRequest
            {
                Messages = [LlmMessage.User("x")],
                ResponseSchema = new LlmJsonSchema("Rating", RatingSchema),
            }, TestContext.Current.CancellationToken)) { }
        });

        Assert.Empty(server.Requests);
    }
}
