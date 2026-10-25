namespace CheapHelpers.Llm;

public enum LlmProvider
{
    OpenRouter = 0,
    OpenAI = 1,
    Anthropic = 2,
    GoogleAiStudio = 3,
    LocalLmStudio = 4,
    Custom = 99,
}

public sealed class LlmOptions
{
    /// <summary>Provider; Anthropic with a non-empty key uses the native Messages API, every other value the OpenAI-compatible client.</summary>
    public LlmProvider Provider { get; set; } = LlmProvider.OpenRouter;

    /// <summary>API key, sent as a bearer token (or x-api-key on the native Anthropic client).</summary>
    public string ApiKey { get; set; } = string.Empty;

    // BaseUrl and Model follow the provider's preset until set, so a key configured for one
    // provider is never sent to another provider's default endpoint.

    /// <summary>Endpoint base URL, defaulting to the provider's preset; the OpenAI-compatible client appends /chat/completions.</summary>
    public string BaseUrl
    {
        get => field ?? LlmProviderPresets.Get(Provider).BaseUrl;
        set => field = value;
    }

    /// <summary>Model id used when <see cref="LlmRequest.Model"/> is empty, defaulting to the provider's preset.</summary>
    public string Model
    {
        get => field ?? LlmProviderPresets.Get(Provider).DefaultModel;
        set => field = value;
    }

    /// <summary>Temperature used when <see cref="LlmRequest.Temperature"/> is null; the native Anthropic client never sends one.</summary>
    public double Temperature { get; set; } = 1.0;

    /// <summary>Output token cap used when <see cref="LlmRequest.MaxTokens"/> is null.</summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>Reasoning effort (low, medium, high, xhigh, max) applied when the request sets none; null sends the model default.</summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>Per-attempt timeout; on the OpenAI-compatible client it caps a whole stream, not the gap between chunks.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Retries after a timeout or a 5xx reply.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Session token cap across all purposes; once reached the clients skip calls until the tracker resets. 0 is unlimited.</summary>
    public long MaxSessionTokens { get; set; }

    /// <summary>Removes Unicode format characters from user and tool-result text before sending; off by default.</summary>
    public bool StripInvisibleCharacters { get; set; }

    /// <summary>Rejects a request whose message text exceeds this many characters before any call is made; 0 is unlimited.</summary>
    public int MaxInputCharacters { get; set; }

    /// <summary>Optional HTTP-Referer header for the OpenAI-compatible client (OpenRouter app attribution).</summary>
    public string? Referer { get; set; }

    /// <summary>Optional X-Title header for the OpenAI-compatible client (OpenRouter app attribution).</summary>
    public string? AppTitle { get; set; }

    /// <summary>Sets <see cref="Provider"/>, <see cref="BaseUrl"/> and <see cref="Model"/> from the provider's preset.</summary>
    public void ApplyPreset(LlmProvider provider)
    {
        var preset = LlmProviderPresets.Get(provider);
        Provider = provider;
        BaseUrl = preset.BaseUrl;
        Model = preset.DefaultModel;
    }
}
