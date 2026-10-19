using System.Text.Json;

namespace CheapHelpers.Llm.Tests;

public sealed class ModelRoutingTests
{
    [Fact]
    public async Task Effort_goes_out_as_openrouter_reasoning_object_on_openrouter()
    {
        var wire = await PostWire(baseUrl: "https://openrouter.ai/api/v1", effort: "low");

        Assert.Equal("low", wire.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.False(wire.TryGetProperty("reasoning_effort", out _));
    }

    [Fact]
    public async Task Effort_goes_out_as_flat_reasoning_effort_everywhere_else()
    {
        var wire = await PostWire(baseUrl: "https://api.openai.com/v1", effort: "high");

        Assert.Equal("high", wire.GetProperty("reasoning_effort").GetString());
        Assert.False(wire.TryGetProperty("reasoning", out _));
    }

    [Fact]
    public async Task No_effort_means_neither_field_is_sent()
    {
        var wire = await PostWire(baseUrl: "https://openrouter.ai/api/v1", effort: null);

        Assert.False(wire.TryGetProperty("reasoning", out _));
        Assert.False(wire.TryGetProperty("reasoning_effort", out _));
    }

    [Fact]
    public async Task Request_effort_wins_over_the_options_effort()
    {
        var wire = await PostWire(baseUrl: "https://api.openai.com/v1", effort: "high", requestEffort: "low");

        Assert.Equal("low", wire.GetProperty("reasoning_effort").GetString());
    }

    private static async Task<JsonElement> PostWire(string baseUrl, string? effort, string? requestEffort = null)
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("ok"));
        var client = TestClients.Compatible(server, new LlmOptions { ApiKey = "k", BaseUrl = baseUrl, ReasoningEffort = effort });

        await client.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")], ReasoningEffort = requestEffort }, TestContext.Current.CancellationToken);

        return JsonDocument.Parse(Assert.Single(server.PostedBodies)).RootElement;
    }
}
