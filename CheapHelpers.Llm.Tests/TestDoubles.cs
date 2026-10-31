using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm.Tests;

internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; set; } = value;
    public T Get(string? name) => CurrentValue;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal static class TestClients
{
    public static OpenAiCompatibleLlmClient Compatible(RecordingHttpHandler server, LlmOptions options, LlmUsageTracker? tracker = null) =>
        new(server.CreateClient(), new TestOptionsMonitor<LlmOptions>(options), tracker ?? new LlmUsageTracker(), NullLogger<OpenAiCompatibleLlmClient>.Instance);

    public static AnthropicLlmClient Anthropic(RecordingHttpHandler server, LlmOptions options, LlmUsageTracker? tracker = null)
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
