using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm.Tests;

/// <summary>Captures every request body and replies with canned bodies in order. Never touches the network.</summary>
internal sealed class FakeLlmServer : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body, string ContentType)> _replies = new();
    public List<string> PostedBodies { get; } = [];
    public List<HttpRequestMessage> Requests { get; } = [];

    public FakeLlmServer Reply(string body, HttpStatusCode status = HttpStatusCode.OK, string contentType = "application/json")
    {
        _replies.Enqueue((status, body, contentType));
        return this;
    }

    public HttpClient CreateClient() => new(this);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        PostedBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
        var (status, body, contentType) = _replies.Dequeue();
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType),
        };
    }
}

internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; set; } = value;
    public T Get(string? name) => CurrentValue;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal static class TestClients
{
    public static OpenAiCompatibleLlmClient Compatible(FakeLlmServer server, LlmOptions options, LlmUsageTracker? tracker = null) =>
        new(server.CreateClient(), new TestOptionsMonitor<LlmOptions>(options), tracker ?? new LlmUsageTracker(), NullLogger<OpenAiCompatibleLlmClient>.Instance);

    public static AnthropicLlmClient Anthropic(FakeLlmServer server, LlmOptions options, LlmUsageTracker? tracker = null)
    {
        var monitor = new TestOptionsMonitor<LlmOptions>(options);
        return new(new AnthropicClientFactory(monitor, server.CreateClient()), monitor, tracker ?? new LlmUsageTracker(), NullLogger<AnthropicLlmClient>.Instance);
    }

    public static string CompatReply(string? content, string finishReason = "stop") => JsonSerializer.Serialize(new
    {
        id = "r1",
        choices = new[] { new { index = 0, message = new { role = "assistant", content }, finish_reason = finishReason } },
        usage = new { prompt_tokens = 10, completion_tokens = 5, total_tokens = 15 },
    });
}
