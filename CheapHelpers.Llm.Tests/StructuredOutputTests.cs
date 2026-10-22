using System.ComponentModel;
using System.Text.Json;

namespace CheapHelpers.Llm.Tests;

public sealed class StructuredOutputTests
{
    public sealed record Verdict(string Name, int Score, string? Note);
    public sealed record Marker;
    public sealed record Wrapper(Marker Inner);

    [Description("A rating of one product.")]
    public sealed record Rated([Description("Score from 1 to 5.")] int Score, string Product, Reviewer By);

    [Description("The person who wrote the review.")]
    public sealed class Reviewer
    {
        [Description("Display name, at most 40 characters.")]
        public string Name { get; set; } = "";
    }

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
    public async Task Objects_without_properties_are_closed_too()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("""{"inner":{}}"""));

        await Client(server).GenerateAsync<Wrapper>(new LlmRequest { Messages = [LlmMessage.User("x")] }, cancellationToken: TestContext.Current.CancellationToken);

        using var body = JsonDocument.Parse(Assert.Single(server.PostedBodies));
        var inner = body.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("schema").GetProperty("properties").GetProperty("inner");
        Assert.False(inner.GetProperty("additionalProperties").GetBoolean());
        Assert.Empty(inner.GetProperty("required").EnumerateArray());
    }

    private static async Task<JsonElement> RatedSchema()
    {
        var server = new FakeLlmServer().Reply(TestClients.CompatReply("""{"score":4,"product":"kettle","by":{"name":"ann"}}"""));
        var request = new LlmRequest { Messages = [LlmMessage.User("rate the kettle")] };

        await Client(server).GenerateAsync<Rated>(request, cancellationToken: TestContext.Current.CancellationToken);

        return request.ResponseSchema!.Schema;
    }

    [Fact]
    public async Task Description_attributes_on_the_type_record_parameters_and_properties_reach_the_schema()
    {
        var schema = await RatedSchema();

        var properties = schema.GetProperty("properties");
        Assert.Equal("A rating of one product.", schema.GetProperty("description").GetString());
        Assert.Equal("Score from 1 to 5.", properties.GetProperty("score").GetProperty("description").GetString());
        Assert.Equal("The person who wrote the review.", properties.GetProperty("by").GetProperty("description").GetString());
        Assert.Equal("Display name, at most 40 characters.", properties.GetProperty("by").GetProperty("properties").GetProperty("name").GetProperty("description").GetString());
    }

    [Fact]
    public async Task Members_without_a_description_get_none_and_the_schema_stays_strict()
    {
        var schema = await RatedSchema();

        Assert.False(schema.GetProperty("properties").GetProperty("product").TryGetProperty("description", out _));
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["score", "product", "by"], schema.GetProperty("required").EnumerateArray().Select(name => name.GetString()));
    }

    [Fact]
    public void Generic_type_names_become_valid_schema_names()
    {
        Assert.Equal("List_1", LlmClientExtensions.SchemaName(typeof(List<Verdict>)));
        Assert.Equal("Verdict", LlmClientExtensions.SchemaName(typeof(Verdict)));
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
