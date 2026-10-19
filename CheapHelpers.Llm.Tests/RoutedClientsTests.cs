using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Llm.Tests;

public sealed class RoutedClientsTests
{
    private const string AnthropicReply = """
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"native"}],
         "stop_reason":"end_turn","stop_sequence":null,
         "usage":{"input_tokens":5,"output_tokens":1}}
        """;

    private static (RoutedLlmClient Router, FakeLlmServer Compat, FakeLlmServer Native) Make(LlmOptions options)
    {
        var compatServer = new FakeLlmServer();
        var nativeServer = new FakeLlmServer();
        var monitor = new TestOptionsMonitor<LlmOptions>(options);
        var router = new RoutedLlmClient(
            monitor,
            TestClients.Compatible(compatServer, options),
            TestClients.Anthropic(nativeServer, options));
        return (router, compatServer, nativeServer);
    }

    [Fact]
    public async Task Anthropic_with_a_key_routes_to_the_native_client()
    {
        var (router, compat, native) = Make(new LlmOptions { Provider = LlmProvider.Anthropic, ApiKey = "k" });
        native.Reply(AnthropicReply);

        var response = await router.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.Equal("native", response.TextContent);
        Assert.Empty(compat.Requests);
    }

    [Fact]
    public async Task Other_providers_route_to_the_compatible_client()
    {
        var (router, compat, native) = Make(new LlmOptions { Provider = LlmProvider.OpenRouter, ApiKey = "k" });
        compat.Reply(TestClients.CompatReply("compat"));

        var response = await router.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken);

        Assert.Equal("compat", response.TextContent);
        Assert.Empty(native.Requests);
    }

    [Fact]
    public void Anthropic_with_an_empty_key_falls_back_to_compatible()
    {
        // The compatible client returns the provider's own error body for a missing key;
        // the SDK would throw an opaque auth error instead.
        Assert.False(RoutedLlmClient.UseNative(new LlmOptions { Provider = LlmProvider.Anthropic, ApiKey = "" }));
        Assert.True(RoutedLlmClient.UseNative(new LlmOptions { Provider = LlmProvider.Anthropic, ApiKey = "k" }));
    }

    [Fact]
    public async Task Switching_the_provider_at_runtime_switches_the_route()
    {
        var options = new LlmOptions { Provider = LlmProvider.OpenRouter, ApiKey = "k" };
        var (router, compat, native) = Make(options);
        compat.Reply(TestClients.CompatReply("compat"));
        native.Reply(AnthropicReply);

        Assert.Equal("compat", (await router.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken)).TextContent);

        options.ApplyPreset(LlmProvider.Anthropic);

        Assert.Equal("native", (await router.GenerateAsync(new LlmRequest { Messages = [LlmMessage.User("hi")] }, TestContext.Current.CancellationToken)).TextContent);
    }

    [Fact]
    public void AddCheapLlm_resolves_the_routed_client_and_one_shared_tracker()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddCheapLlm(options => options.ApplyPreset(LlmProvider.Anthropic))
            .BuildServiceProvider();

        Assert.IsType<RoutedLlmClient>(provider.GetRequiredService<ILlmClient>());
        Assert.Same(provider.GetRequiredService<AnthropicClientFactory>(), provider.GetRequiredService<AnthropicClientFactory>());
        Assert.Same(provider.GetRequiredService<LlmUsageTracker>(), provider.GetRequiredService<LlmUsageTracker>());
        Assert.Equal(LlmProviderPresets.Get(LlmProvider.Anthropic).BaseUrl, provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<LlmOptions>>().CurrentValue.BaseUrl);
    }
}
