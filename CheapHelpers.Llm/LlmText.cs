using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CheapHelpers.Llm;

/// <summary>Text hygiene for untrusted input sent to a model.</summary>
public static class LlmText
{
    private const int ArabicNumberSignFirst = 0x0600;
    private const int ArabicNumberSignLast = 0x0605;
    private const int ArabicEndOfAyah = 0x06DD;
    private const int ZeroWidthNonJoiner = 0x200C;
    private const int ZeroWidthJoiner = 0x200D;
    private const string UserRole = "user";
    private const string ToolRole = "tool";

    /// <summary>Removes Unicode format characters (zero-width, bidi controls, soft hyphen, BOM, tag characters). The zero-width joiner and non-joiner stay, so joined emoji and scripts that spell with them survive; tag-based subdivision flags do not. The Arabic number signs (U+0600 to U+0605, U+06DD) stay because they render.</summary>
    public static string StripInvisible(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        StringBuilder? builder = null;
        var index = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var length = rune.Utf16SequenceLength;
            if (IsInvisible(rune))
                builder ??= new StringBuilder(text.Length).Append(text, 0, index);
            else
                // Copies from the source by index, so a lone surrogate (enumerated as U+FFFD) is kept as it was.
                builder?.Append(text, index, length);
            index += length;
        }
        return builder?.ToString() ?? text;
    }

    /// <summary>Enforces <see cref="LlmOptions.StripInvisibleCharacters"/> and then <see cref="LlmOptions.MaxInputCharacters"/>; returns the same request when nothing changed.</summary>
    internal static LlmRequest ApplyInputRules(LlmRequest request, LlmOptions config)
    {
        if (config.StripInvisibleCharacters)
        {
            var messages = request.Messages.Select(Clean).ToList();
            // LlmMessage has reference equality, so this is false only when a message was replaced.
            if (!messages.SequenceEqual(request.Messages))
                request = request.WithMessages(messages);
        }

        // Counted after stripping, so the limit applies to the text that is sent.
        if (config.MaxInputCharacters > 0)
        {
            var characters = request.Messages.Sum(message => (long)(TextOf(message.Content)?.Length ?? 0));
            if (characters > config.MaxInputCharacters)
                throw new ArgumentException($"Request message text is {characters} characters, over the limit of {config.MaxInputCharacters}.", nameof(request));
        }

        return request;
    }

    private static bool IsInvisible(Rune rune) =>
        rune.Value is not (ZeroWidthNonJoiner or ZeroWidthJoiner or ArabicEndOfAyah or (>= ArabicNumberSignFirst and <= ArabicNumberSignLast))
        && Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format;

    // A message deserialised with System.Text.Json (a reloaded chat history) holds its text as a JsonElement.
    // Multimodal content-part arrays are left alone and not counted; callers clean those parts with StripInvisible.
    private static string? TextOf(object? content) => content switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null,
    };

    private static LlmMessage Clean(LlmMessage message)
    {
        if (message.Role is not (UserRole or ToolRole) || TextOf(message.Content) is not { } text)
            return message;

        var clean = StripInvisible(text);
        if (ReferenceEquals(clean, text))
            return message;

        // A copy, so the caller's message keeps its original text. New LlmMessage properties must be added here too.
        return new LlmMessage
        {
            Role = message.Role,
            Content = clean,
            ToolCalls = message.ToolCalls,
            ToolCallId = message.ToolCallId,
            Name = message.Name,
            ProviderBlocks = message.ProviderBlocks,
        };
    }
}
