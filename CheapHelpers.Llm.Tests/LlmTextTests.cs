using System.Text.Json;

namespace CheapHelpers.Llm.Tests;

public sealed class LlmTextTests
{
    // Zero-width space, word joiner, right-to-left override, BOM, soft hyphen, invisible times, tag character.
    private const string Dirty = "one\u200B two\u2060 three\u202E four\uFEFF five\u00AD six\u2062 seven\U000E0041";
    private const string Clean = "one two three four five six seven";
    private const string Family = "\U0001F468‍\U0001F469‍\U0001F467";

    [Fact]
    public void StripInvisible_removes_format_characters()
    {
        Assert.Equal(Clean, LlmText.StripInvisible(Dirty));
    }

    [Fact]
    public void StripInvisible_keeps_the_zero_width_joiner_and_returns_the_same_instance()
    {
        Assert.Same(Family, LlmText.StripInvisible(Family));
    }

    [Fact]
    public void StripInvisible_keeps_the_zero_width_non_joiner()
    {
        // Persian spells with U+200C; stripping it would change the word.
        const string persian = "\u0645\u06CC\u200C\u062E\u0648\u0627\u0647\u0645";
        Assert.Same(persian, LlmText.StripInvisible(persian));
    }

    [Fact]
    public void StripInvisible_keeps_the_arabic_number_signs()
    {
        // U+0600 and U+06DD are format characters that render; a number sign before a digit and the end of ayah mark.
        const string arabic = "؀١ ۝٢";
        Assert.Same(arabic, LlmText.StripInvisible(arabic));
    }

    [Fact]
    public void StripInvisible_returns_plain_text_and_empty_unchanged()
    {
        const string plain = "plain ascii text";
        Assert.Same(plain, LlmText.StripInvisible(plain));
        Assert.Equal(string.Empty, LlmText.StripInvisible(string.Empty));
    }

    [Fact]
    public void ApplyInputRules_with_both_options_off_returns_the_same_request()
    {
        var request = new LlmRequest { Messages = [LlmMessage.User(Dirty)] };

        Assert.Same(request, LlmText.ApplyInputRules(request, new LlmOptions()));
    }

    [Fact]
    public void ApplyInputRules_cleans_user_and_tool_text_only_and_leaves_the_callers_messages_alone()
    {
        var system = LlmMessage.System(Dirty);
        var user = LlmMessage.User(Dirty);
        var tool = LlmMessage.ToolResult("t1", "lookup", Dirty);
        var request = new LlmRequest { Messages = [system, user, tool], Purpose = UsagePurpose.Chat };

        var applied = LlmText.ApplyInputRules(request, new LlmOptions { StripInvisibleCharacters = true });

        Assert.NotSame(request, applied);
        Assert.Same(system, applied.Messages[0]);
        Assert.Equal(Clean, applied.Messages[1].Content);
        Assert.Equal(Clean, applied.Messages[2].Content);
        Assert.Equal("t1", applied.Messages[2].ToolCallId);
        Assert.Equal(Dirty, user.Content);
        Assert.Equal(Dirty, tool.Content);
        Assert.Equal(UsagePurpose.Chat, applied.Purpose);
    }

    [Fact]
    public void ApplyInputRules_handles_deserialised_json_text()
    {
        var message = JsonSerializer.Deserialize<LlmMessage>("""{"role":"user","content":"hel​lo"}""")!;
        var request = new LlmRequest { Messages = [message] };

        var applied = LlmText.ApplyInputRules(request, new LlmOptions { StripInvisibleCharacters = true });

        Assert.Equal("hello", applied.Messages[0].Content);
        Assert.Throws<ArgumentException>(() => LlmText.ApplyInputRules(request, new LlmOptions { MaxInputCharacters = 5 }));
    }

    [Fact]
    public void ApplyInputRules_rejects_text_over_the_character_limit()
    {
        var request = new LlmRequest { Messages = [LlmMessage.User("01234567890")] };

        var ex = Assert.Throws<ArgumentException>(() => LlmText.ApplyInputRules(request, new LlmOptions { MaxInputCharacters = 10 }));

        Assert.Equal("request", ex.ParamName);
        Assert.Contains("10", ex.Message);
        Assert.Contains("11", ex.Message);
    }

    [Fact]
    public void ApplyInputRules_counts_characters_after_stripping()
    {
        // Ten visible characters padded with five zero-width spaces: over the raw limit, within it once cleaned.
        var request = new LlmRequest { Messages = [LlmMessage.User("0123456789​​​​​")] };
        var config = new LlmOptions { StripInvisibleCharacters = true, MaxInputCharacters = 10 };

        var applied = LlmText.ApplyInputRules(request, config);

        Assert.Equal("0123456789", applied.Messages[0].Content);
    }
}
