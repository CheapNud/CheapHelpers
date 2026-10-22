# CheapHelpers.Llm Documentation

LLM client for Anthropic and OpenAI-compatible providers with structured output and a usage cap. One `ILlmClient` serves OpenRouter, OpenAI, Google AI Studio, LM Studio, any custom OpenAI-compatible endpoint, and Anthropic's native Messages API.

## Package Information

- **Package**: CheapHelpers.Llm
- **Version**: 4.0.0
- **Target Framework**: .NET 11.0 (net11.0)
- **NuGet**: [CheapHelpers.Llm](https://www.nuget.org/packages/CheapHelpers.Llm)

## Usage

### Register

```csharp
using CheapHelpers.Llm;

builder.Services.AddCheapLlm(options =>
{
    options.ApplyPreset(LlmProvider.OpenRouter);
    options.ApiKey = builder.Configuration["Llm:ApiKey"]!;
});
```

`AddCheapLlm` registers `ILlmClient`, the `LlmUsageTracker` singleton and both clients behind it. The router reads the options on every call: `Provider = Anthropic` with a non-empty key goes to the native Messages API (prompt caching, effort, thinking blocks kept across tool rounds), every other setting goes to the OpenAI-compatible client.

### Options

| Option | Default | Meaning |
|--------|---------|---------|
| `Provider` | `OpenRouter` | Picks the client; see Register |
| `ApiKey` | `""` | Bearer token, or the Anthropic API key |
| `BaseUrl` | The provider's preset | Endpoint base; the compatible client appends `/chat/completions` |
| `Model` | The provider's preset | Used when `LlmRequest.Model` is empty |
| `Temperature` | `1.0` | Used when `LlmRequest.Temperature` is null; the Anthropic client never sends it |
| `MaxTokens` | `4096` | Used when `LlmRequest.MaxTokens` is null |
| `ReasoningEffort` | `null` | `low`, `medium`, `high`, `xhigh` or `max`; a request's own effort wins |
| `TimeoutSeconds` | `60` | Per attempt; on the compatible client it caps a whole stream |
| `MaxRetries` | `2` | Retries after a timeout or a 5xx reply |
| `MaxSessionTokens` | `0` | Session token cap; `0` is unlimited |
| `Referer` | `null` | `HTTP-Referer` header for OpenRouter app attribution |
| `AppTitle` | `null` | `X-Title` header for OpenRouter app attribution |

Until you set them, `BaseUrl` and `Model` follow the preset of the current `Provider` (OpenRouter and `mistralai/mistral-large-4-0` by default), so a key set for one provider never goes to another provider's endpoint.

The compatible client sends the effort as OpenRouter's `reasoning: { effort }` when the base URL host ends with `openrouter.ai`, and as OpenAI's flat `reasoning_effort` everywhere else.

### Presets

`ApplyPreset(provider)` sets `Provider`, `BaseUrl` and `Model`. `LlmProviderPresets.Get(provider)` also gives the key page and suggested models, for a settings screen.

| Provider | Base URL | Default model |
|----------|----------|---------------|
| `OpenRouter` | `https://openrouter.ai/api/v1` | `mistralai/mistral-large-4-0` |
| `OpenAI` | `https://api.openai.com/v1` | `gpt-4o` |
| `Anthropic` | `https://api.anthropic.com/v1` | `claude-sonnet-5-5` |
| `GoogleAiStudio` | `https://generativelanguage.googleapis.com/v1beta/openai` | `gemini-2.5-flash` |
| `LocalLmStudio` | `http://localhost:1234/v1` | `local-model` |
| `Custom` | (set your own) | (set your own) |

The OpenRouter suggestions are one recent model per vendor, each of which supports tools and strict JSON schema output, so structured output works on all of them:

| Constant | Model id |
|----------|----------|
| `OpenRouterMistral` | `mistralai/mistral-large-4-0` |
| `OpenRouterClaudeSonnet` | `anthropic/claude-sonnet-5.5` |
| `OpenRouterClaudeHaiku` | `anthropic/claude-haiku-5.5` |
| `OpenRouterOpenAi` | `openai/gpt-6.1-sol` |
| `OpenRouterGemini` | `google/gemini-4-argon` |
| `OpenRouterDeepSeek` | `deepseek/deepseek-v4.1-flash-1017` |
| `OpenRouterGlm` | `z-ai/glm-5.3-flash` |
| `OpenRouterQwen` | `qwen/qwen3.8-max-0902` |

### Generate

```csharp
var response = await llm.GenerateAsync(new LlmRequest
{
    Messages = [LlmMessage.System("Answer in one sentence."), LlmMessage.User("Why is the sky blue?")],
}, cancellationToken);

Debug.WriteLine(response.TextContent);
```

Tools go in `LlmRequest.Tools` (`LlmToolDefinition.Create(name, description, schema)`). Calls come back in `response.ToolCalls`; answer them with `LlmMessage.AssistantToolCalls(...)` and `LlmMessage.ToolResult(...)`. Keep the assistant message's `ProviderBlocks` when you build the next round, so the native Anthropic client can replay its thinking blocks.

### Structured output

`GenerateAsync<T>` builds a JSON schema for `T` with the BCL's `JsonSchemaExporter`, asks the model for a matching reply and deserialises it. `T` must serialise as a JSON object, such as a class or record.

```csharp
public sealed record Rating(string Product, int Score, string? Comment);

var rating = await llm.GenerateAsync<Rating>(new LlmRequest
{
    Messages = [LlmMessage.User("Rate this review: 'Great kettle, a bit loud.'")],
}, cancellationToken: cancellationToken);
```

The compatible client sends the schema as a strict `response_format` of type `json_schema`; the Anthropic client sends it as a tool named after the type and forces that tool through `tool_choice`. Both put the reply in `LlmResponse.StructuredJson`. `GenerateAsync<T>` throws `InvalidOperationException`, naming the finish reason, when the model gave no JSON (for example when it hit `MaxTokens`). The default serializer options are `JsonSerializerDefaults.Web`; pass your own as `jsonOptions`.

To send a hand-written schema, set `LlmRequest.ResponseSchema = new LlmJsonSchema(name, schemaElement)` and read `response.StructuredJson`.

### Usage cap

Every reply's token usage is recorded in `LlmUsageTracker` under `LlmRequest.Purpose` (default `UsagePurpose.Chat`; use your own strings for other buckets). Once the total reaches `MaxSessionTokens`, both clients skip the call: `GenerateAsync` returns an empty `LlmResponse` and `GenerateStreamAsync` yields nothing. Cache reads on the native Anthropic client count a tenth toward the cap, matching how they bill.

```csharp
var snapshot = tracker.Snapshot();
Debug.WriteLine($"{snapshot.Total.TotalTokens} tokens over {snapshot.Total.Calls} calls");
tracker.Reset();
```

### Streaming

```csharp
await foreach (var chunk in llm.GenerateStreamAsync(request, cancellationToken))
{
    if (chunk.ContentDelta is { } text)
        Debug.Write(text);
}
```

Chunks carry text fragments, tool call fragments (`ToolCallDeltas`, tied together by `Index`) and the finish reason. The native Anthropic client's last chunk also carries `ProviderBlocks` for the next tool round.

- Streaming does not take `ResponseSchema`: both clients throw `NotSupportedException` when it is set. Use `GenerateAsync` or `GenerateAsync<T>` for structured output.
- On the compatible client `TimeoutSeconds` caps the whole stream, not the gap between chunks, so raise it for long replies.

## Dependencies

- Anthropic (12.58.0), the official SDK behind the native client
- Microsoft.Extensions.Http (10.0.13), for the typed `HttpClient` of the compatible client
