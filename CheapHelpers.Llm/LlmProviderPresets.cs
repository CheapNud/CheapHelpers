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
    public const string OpenRouterDefaultModel = "anthropic/claude-sonnet-5.5";

    public static IReadOnlyDictionary<LlmProvider, LlmProviderPreset> All { get; } =
        new Dictionary<LlmProvider, LlmProviderPreset>
        {
            [LlmProvider.OpenRouter] = new(
                "OpenRouter",
                OpenRouterBaseUrl,
                OpenRouterDefaultModel,
                "https://openrouter.ai/keys",
                [
                    "anthropic/claude-sonnet-5.5",
                    "anthropic/claude-opus-5.5",
                    "anthropic/claude-haiku-5.5",
                    "openai/gpt-4o",
                    "openai/gpt-4o-mini",
                    "google/gemini-2.5-flash",
                    "google/gemini-2.5-pro",
                    "meta-llama/llama-3.3-70b-instruct",
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
