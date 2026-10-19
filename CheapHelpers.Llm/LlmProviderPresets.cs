namespace CheapHelpers.Llm;

public sealed record LlmProviderPreset(
    string DisplayName,
    string BaseUrl,
    string DefaultModel,
    string KeyUrl,
    IReadOnlyList<string> SuggestedModels);

/// <summary>Base URL, default model, key page and suggested models per provider.</summary>
public static class LlmProviderPresets
{
    public const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1";

    // OpenRouter ids checked on 2026-10-19: each lists tools, response_format and structured_outputs.
    public const string OpenRouterMistral = "mistralai/mistral-large-4-0";
    public const string OpenRouterClaudeSonnet = "anthropic/claude-sonnet-5.5";
    public const string OpenRouterClaudeHaiku = "anthropic/claude-haiku-5.5";
    public const string OpenRouterOpenAi = "openai/gpt-6.1-sol";
    public const string OpenRouterGemini = "google/gemini-4-argon";
    public const string OpenRouterDeepSeek = "deepseek/deepseek-v4.1-flash-1017";
    public const string OpenRouterGlm = "z-ai/glm-5.3-flash";
    public const string OpenRouterQwen = "qwen/qwen3.8-max-0902";

    public const string OpenRouterDefaultModel = OpenRouterMistral;

    public static IReadOnlyDictionary<LlmProvider, LlmProviderPreset> All { get; } =
        new Dictionary<LlmProvider, LlmProviderPreset>
        {
            [LlmProvider.OpenRouter] = new(
                "OpenRouter",
                OpenRouterBaseUrl,
                OpenRouterDefaultModel,
                "https://openrouter.ai/keys",
                [
                    OpenRouterMistral,
                    OpenRouterClaudeSonnet,
                    OpenRouterClaudeHaiku,
                    OpenRouterOpenAi,
                    OpenRouterGemini,
                    OpenRouterDeepSeek,
                    OpenRouterGlm,
                    OpenRouterQwen,
                ]),
            [LlmProvider.OpenAI] = new(
                "OpenAI",
                "https://api.openai.com/v1",
                "gpt-4o",
                "https://platform.openai.com/api-keys",
                ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "o1-mini", "o1-preview"]),
            [LlmProvider.Anthropic] = new(
                "Anthropic",
                "https://api.anthropic.com/v1",
                "claude-sonnet-5-5",
                "https://console.anthropic.com/settings/keys",
                ["claude-opus-5-5", "claude-sonnet-5-5", "claude-haiku-5-5"]),
            [LlmProvider.GoogleAiStudio] = new(
                "Google AI Studio",
                "https://generativelanguage.googleapis.com/v1beta/openai",
                "gemini-2.5-flash",
                "https://aistudio.google.com/apikey",
                ["gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.5-flash-lite"]),
            [LlmProvider.LocalLmStudio] = new(
                "Local LM Studio",
                "http://localhost:1234/v1",
                "local-model",
                "https://lmstudio.ai/",
                ["local-model"]),
            [LlmProvider.Custom] = new(
                "Custom",
                string.Empty,
                string.Empty,
                string.Empty,
                []),
        };

    public static LlmProviderPreset Get(LlmProvider provider) => All[provider];
}
