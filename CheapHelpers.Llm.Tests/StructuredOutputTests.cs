using System.Text.Json;

namespace CheapHelpers.Llm.Tests;

public sealed class StructuredOutputTests
{
    public sealed record Verdict(string Name, int Score, string? Note);

    private static OpenAiCompatibleLlmClient Client(FakeLlmServer server) =>
        TestClients.Compatible(server, new LlmOptions { ApiKey = "k", BaseUrl = "https://openrouter.ai/api/v1" });

    [Fact]
    public async Task GenerateAsync_of_T_round_trips_a_record()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("""{"name":"tea","score":4,"note":null}"""));
        var request = new LlmRequest { Messages = [LlmMessage.User("rate tea")] };

        var verdict = await Client(server).GenerateAsync<Verdict>(request, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new Verdict("tea", 4, null), verdict);
        Assert.Equal("Verdict", request.ResponseSchema!.Name);
    }

    [Fact]
    public async Task GenerateAsync_of_T_sends_a_strict_schema_with_every_property_required()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("""{"name":"tea","score":4,"note":"ok"}"""));

        await Client(server).GenerateAsync<Verdict>(new LlmRequest { Messages = [LlmMessage.User("rate tea")] }, cancellationToken: TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(server.PostedBodies));
        var schema = body.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("schema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["name", "score", "note"], schema.GetProperty("required").EnumerateArray().Select(name => name.GetString()));
    }

    [Fact]
    public async Task GenerateAsync_of_T_throws_with_the_finish_reason_when_no_json_came_back()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply(null, finishReason: "length"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Client(server).GenerateAsync<Verdict>(new LlmRequest { Messages = [LlmMessage.User("rate tea")] }, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("length", ex.Message);
    }
}
