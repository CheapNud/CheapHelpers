using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Llm;

/// <summary>
/// Chat client for any OpenAI-compatible /chat/completions endpoint: OpenRouter, OpenAI,
/// Google AI Studio's OpenAI endpoint, LM Studio or a custom server.
/// </summary>
public sealed class OpenAiCompatibleLlmClient(
    HttpClient httpClient,
    IOptionsMonitor<LlmOptions> options,
    LlmUsageTracker usageTracker,
    ILogger<OpenAiCompatibleLlmClient> logger) : ILlmClient
{
    private const string ChatCompletionsPath = "/chat/completions";
    private const string JsonMediaType = "application/json";
    private const string BearerScheme = "Bearer";
    private const string RefererHeader = "HTTP-Referer";
    private const string TitleHeader = "X-Title";
    private const string OpenRouterHost = "openrouter.ai";
    private const string SseDataPrefix = "data:";
    private const string SseDone = "[DONE]";
    private const int RetryDelayStepMs = 1000;
    private const int FirstServerErrorStatus = 500;
    private const string ResponseFormatField = "response_format";
    private const string JsonSchemaFormat = "json_schema";
    internal const string StreamingSchemaMessage = "Streaming does not support ResponseSchema; use GenerateAsync for structured output.";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = options.CurrentValue;
        if (usageTracker.IsOverBudget(config.MaxSessionTokens))
        {
            logger.LogWarning("Session token budget {MaxSessionTokens} reached, skipping call (purpose={Purpose})", config.MaxSessionTokens, request.Purpose);
            return new LlmResponse();
        }

        var endpoint = Endpoint(config);
        var wireRequest = ToWireRequest(request, config, stream: false);
        // Serialised once; every attempt gets a fresh HttpRequestMessage over the same bytes,
        // since a request message can only be sent once.
        var body = Serialize(wireRequest, request.ResponseSchema);
        logger.LogDebug("POST {Endpoint} | model={Model} | messages={Messages} | tools={Tools}", endpoint, wireRequest.Model, request.Messages.Count, request.Tools?.Count ?? 0);

        var retries = 0;
        while (true)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

                using var httpRequest = CreateHttpRequest(endpoint, body, config);
                using var httpResponse = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
                var responseBody = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var status = (int)httpResponse.StatusCode;
                    logger.LogWarning("HTTP {Status}: {Body}", status, responseBody);

                    if (retries < config.MaxRetries && status >= FirstServerErrorStatus)
                    {
                        retries++;
                        await Task.Delay(RetryDelayStepMs * retries, cancellationToken);
                        continue;
                    }

                    throw new HttpRequestException($"LLM API returned {status}: {responseBody}");
                }

                var llmResponse = JsonSerializer.Deserialize<LlmResponse>(responseBody, SerializerOptions)
                    ?? throw new JsonException("Failed to deserialize LLM response");

                if (request.ResponseSchema is not null)
                    llmResponse.StructuredJson = llmResponse.TextContent;

                logger.LogDebug("Response: finish={FinishReason} | tools={HasToolCalls} | usage={TotalTokens}", llmResponse.FinishReason, llmResponse.HasToolCalls, llmResponse.Usage?.TotalTokens ?? 0);
                usageTracker.Record(request.Purpose, llmResponse.Usage);
                return llmResponse;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Request timed out after {TimeoutSeconds}s", config.TimeoutSeconds);
                if (retries < config.MaxRetries)
                {
                    retries++;
                    continue;
                }
                throw new TimeoutException($"LLM request timed out after {config.TimeoutSeconds}s");
            }
        }
    }

    public async IAsyncEnumerable<LlmStreamChunk> GenerateStreamAsync(
        LlmRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.ResponseSchema is not null)
            throw new NotSupportedException(StreamingSchemaMessage);

        var config = options.CurrentValue;
        if (usageTracker.IsOverBudget(config.MaxSessionTokens))
        {
            logger.LogWarning("Session token budget {MaxSessionTokens} reached, skipping stream (purpose={Purpose})", config.MaxSessionTokens, request.Purpose);
            yield break;
        }

        var endpoint = Endpoint(config);
        var body = Serialize(ToWireRequest(request, config, stream: true), responseSchema: null);
        logger.LogDebug("POST {Endpoint} (stream) | messages={Messages}", endpoint, request.Messages.Count);

        // Covers the whole stream, not just the headers: documented on LlmOptions.TimeoutSeconds.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

        using var httpRequest = CreateHttpRequest(endpoint, body, config);
        // ResponseHeadersRead so parsing starts as soon as the headers land instead of
        // waiting for the entire body.
        using var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);
            logger.LogWarning("HTTP {Status} on stream: {Body}", (int)httpResponse.StatusCode, errorBody);
            throw new HttpRequestException($"LLM API returned {(int)httpResponse.StatusCode}: {errorBody}");
        }

        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(timeoutCts.Token);
        using var reader = new StreamReader(responseStream, Encoding.UTF8);

        // ReadLineAsync returns null at end of stream; preferred over EndOfStream in async
        // code per CA2024 (EndOfStream blocks synchronously to peek).
        while (true)
        {
            timeoutCts.Token.ThrowIfCancellationRequested();

            string? line;
            try
            {
                line = await reader.ReadLineAsync(timeoutCts.Token);
            }
            catch (IOException ex)
            {
                // Socket fault mid-stream, for example the provider dropping the connection
                // after a billing error. End the stream cleanly so the caller sees no more
                // chunks instead of an IOException. (yield break cannot live in a catch.)
                logger.LogWarning(ex, "Transport read failed, ending stream");
                line = null;
            }

            if (line is null) yield break;
            if (!line.StartsWith(SseDataPrefix, StringComparison.Ordinal)) continue;

            var payload = line[SseDataPrefix.Length..].Trim();
            if (payload.Length == 0) continue;
            if (payload == SseDone) yield break;

            LlmStreamChunk? chunk;
            try
            {
                chunk = ParseChunk(payload, request.Purpose);
            }
            catch (JsonException ex)
            {
                // The provider may emit comments or keepalives this parser does not know;
                // skip them rather than tearing the whole stream down.
                logger.LogDebug(ex, "Skipped malformed stream chunk");
                continue;
            }

            if (chunk is not null)
                yield return chunk;
        }
    }

    private static string Endpoint(LlmOptions config) => $"{config.BaseUrl.TrimEnd('/')}{ChatCompletionsPath}";

    /// <summary>
    /// Copy of the caller's request with the option defaults filled in. The caller's instance
    /// is often reused across tool rounds, so it is never mutated.
    /// </summary>
    private static LlmRequest ToWireRequest(LlmRequest request, LlmOptions config, bool stream)
    {
        var applyEffort = request.Reasoning is null && request.ReasoningEffort is null && !string.IsNullOrWhiteSpace(config.ReasoningEffort);
        // OpenRouter only forwards effort to Claude through its own "reasoning" object;
        // everyone else speaks OpenAI's flat "reasoning_effort".
        var isOpenRouter = IsOpenRouter(config.BaseUrl);

        return new LlmRequest
        {
            Model = string.IsNullOrEmpty(request.Model) ? config.Model : request.Model,
            Messages = request.Messages,
            Tools = request.Tools,
            Temperature = request.Temperature ?? config.Temperature,
            MaxTokens = request.MaxTokens ?? config.MaxTokens,
            Reasoning = applyEffort && isOpenRouter ? new ReasoningOptions { Effort = config.ReasoningEffort! } : request.Reasoning,
            ReasoningEffort = applyEffort && !isOpenRouter ? config.ReasoningEffort : request.ReasoningEffort,
            Stream = stream,
            // A trailing usage chunk keeps token spend tracked on the streaming path too.
            StreamOptions = stream ? new StreamOptions { IncludeUsage = true } : null,
            Purpose = request.Purpose,
        };
    }

    /// <summary>
    /// Serialises the wire request, adding a strict json_schema response_format when a schema
    /// is given. <see cref="LlmRequest.ResponseSchema"/> is not serialised itself because the
    /// Anthropic client sends the same schema as a forced tool instead.
    /// </summary>
    private static byte[] Serialize(LlmRequest wireRequest, LlmJsonSchema? responseSchema)
    {
        if (responseSchema is null)
            return JsonSerializer.SerializeToUtf8Bytes(wireRequest, SerializerOptions);

        var node = JsonSerializer.SerializeToNode(wireRequest, SerializerOptions)!.AsObject();
        node[ResponseFormatField] = new JsonObject
        {
            ["type"] = JsonSchemaFormat,
            [JsonSchemaFormat] = new JsonObject
            {
                ["name"] = responseSchema.Name,
                ["strict"] = true,
                ["schema"] = JsonSerializer.SerializeToNode(responseSchema.Schema),
            },
        };
        return JsonSerializer.SerializeToUtf8Bytes(node, SerializerOptions);
    }

    internal static bool IsOpenRouter(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
        && uri.Host.EndsWith(OpenRouterHost, StringComparison.OrdinalIgnoreCase);

    private static HttpRequestMessage CreateHttpRequest(string endpoint, byte[] body, LlmOptions config)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new ByteArrayContent(body),
        };
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType) { CharSet = Encoding.UTF8.WebName };

        if (!string.IsNullOrEmpty(config.ApiKey))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, config.ApiKey);
        if (!string.IsNullOrEmpty(config.Referer))
            httpRequest.Headers.TryAddWithoutValidation(RefererHeader, config.Referer);
        if (!string.IsNullOrEmpty(config.AppTitle))
            httpRequest.Headers.TryAddWithoutValidation(TitleHeader, config.AppTitle);

        return httpRequest;
    }

    private LlmStreamChunk? ParseChunk(string payload, string purpose)
    {
        using var doc = JsonDocument.Parse(payload);

        var hasChoices = doc.RootElement.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0;

        // Usage arrives on the dedicated trailing chunk (empty choices) when
        // stream_options.include_usage is set. Record only from that chunk: some
        // providers also echo usage on the finish_reason chunk, and recording from
        // both would double-count the stream's tokens.
        if (!hasChoices)
        {
            if (doc.RootElement.TryGetProperty("usage", out var usageElement) &&
                usageElement.ValueKind == JsonValueKind.Object)
            {
                usageTracker.Record(purpose, usageElement.Deserialize<LlmUsage>(SerializerOptions));
            }

            return null;
        }

        var first = choices[0];

        string? contentDelta = null;
        List<LlmToolCallDelta>? toolCallDeltas = null;
        if (first.TryGetProperty("delta", out var delta))
        {
            if (delta.TryGetProperty("content", out var contentElement) &&
                contentElement.ValueKind == JsonValueKind.String)
            {
                contentDelta = contentElement.GetString();
            }

            if (delta.TryGetProperty("tool_calls", out var toolCallsElement) &&
                toolCallsElement.ValueKind == JsonValueKind.Array)
            {
                toolCallDeltas = ParseToolCallDeltas(toolCallsElement);
            }
        }

        string? finishReason = null;
        if (first.TryGetProperty("finish_reason", out var finishElement) &&
            finishElement.ValueKind == JsonValueKind.String)
        {
            finishReason = finishElement.GetString();
        }

        if (contentDelta is null && toolCallDeltas is null && finishReason is null)
            return null;

        return new LlmStreamChunk(contentDelta, toolCallDeltas, finishReason);
    }

    private static List<LlmToolCallDelta> ParseToolCallDeltas(JsonElement toolCallsArray)
    {
        var deltas = new List<LlmToolCallDelta>(toolCallsArray.GetArrayLength());
        foreach (var element in toolCallsArray.EnumerateArray())
        {
            var index = element.TryGetProperty("index", out var idx) && idx.TryGetInt32(out var i) ? i : 0;
            var id = element.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : null;
            string? name = null;
            string? argsDelta = null;
            if (element.TryGetProperty("function", out var func))
            {
                if (func.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                    name = nameEl.GetString();
                if (func.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.String)
                    argsDelta = argsEl.GetString();
            }
            deltas.Add(new LlmToolCallDelta(index, id, name, argsDelta));
        }
        return deltas;
    }
}
