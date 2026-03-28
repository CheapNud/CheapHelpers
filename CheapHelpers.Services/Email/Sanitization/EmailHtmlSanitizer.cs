using System.Text.RegularExpressions;
using Ganss.Xss;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Email.Sanitization;

/// <summary>
/// Validates and sanitizes user-authored HTML for safe email inclusion.
/// Uses HtmlSanitizer with email-safe defaults and preserves Liquid template variables
/// through placeholder substitution during the sanitization pass.
/// </summary>
public partial class EmailHtmlSanitizer(EmailHtmlSanitizerOptions sanitizerOptions, ILogger<EmailHtmlSanitizer>? logger = null) : IEmailHtmlSanitizer
{
    private static readonly string[] EmailSafeTags =
    [
        "a", "abbr", "b", "blockquote", "br", "center", "cite", "code",
        "dd", "div", "dl", "dt", "em", "font",
        "h1", "h2", "h3", "h4", "h5", "h6", "hr",
        "i", "img", "li", "ol",
        "p", "pre", "s", "small", "span", "strong", "sub", "sup",
        "table", "tbody", "td", "tfoot", "th", "thead", "tr",
        "u", "ul",
    ];

    private static readonly string[] EmailSafeAttributes =
    [
        "align", "alt", "bgcolor", "border",
        "cellpadding", "cellspacing", "class", "color", "colspan",
        "dir", "face", "height", "href", "id", "lang",
        "rowspan", "size", "src", "style", "target", "title",
        "valign", "width",
    ];

    private static readonly string[] EmailSafeSchemes = ["http", "https", "mailto", "data"];

    [GeneratedRegex(@"\{\{\s*(?<variable>[A-Za-z_.]+)(?:\s*\|\s*[^}]*)?\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex LiquidVariablePattern();

    public EmailSanitizationResult Validate(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new EmailSanitizationResult { IsValid = true };

        var unknownVars = ValidateLiquidVariables(html);

        // Dry-run sanitization to detect what would be removed
        var (_, removedItems) = SanitizeHtml(html);

        var isValid = unknownVars.Count == 0 && removedItems.Count == 0;

        return new EmailSanitizationResult
        {
            IsValid = isValid,
            RemovedItems = removedItems,
            UnknownLiquidVariables = unknownVars,
        };
    }

    public EmailSanitizationResult Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new EmailSanitizationResult { IsValid = true, SanitizedHtml = html ?? string.Empty };

        var unknownVars = ValidateLiquidVariables(html);

        var (sanitizedHtml, removedItems) = SanitizeHtml(html);

        if (removedItems.Count > 0)
            logger?.LogWarning("HTML sanitization removed {Count} items: {Items}", removedItems.Count, string.Join(", ", removedItems));

        return new EmailSanitizationResult
        {
            IsValid = unknownVars.Count == 0,
            SanitizedHtml = sanitizedHtml,
            RemovedItems = removedItems,
            UnknownLiquidVariables = unknownVars,
        };
    }

    private List<string> ValidateLiquidVariables(string html)
    {
        if (sanitizerOptions.AllowedLiquidVariables.Count == 0)
            return [];

        var unknownVars = new List<string>();
        var matches = LiquidVariablePattern().Matches(html);

        foreach (Match match in matches)
        {
            var variableName = match.Groups["variable"].Value;
            if (!sanitizerOptions.AllowedLiquidVariables.Contains(variableName))
                unknownVars.Add(variableName);
        }

        if (unknownVars.Count > 0)
            logger?.LogWarning("Unknown Liquid variables found: {Variables}", string.Join(", ", unknownVars));

        return unknownVars;
    }

    private (string SanitizedHtml, List<string> RemovedItems) SanitizeHtml(string html)
    {
        var removedItems = new List<string>();
        var sanitizer = CreateSanitizer(removedItems);

        // Replace Liquid variables with safe placeholders before sanitizing
        var (placeholderHtml, placeholderMap) = ReplaceLiquidWithPlaceholders(html);

        var sanitizedHtml = sanitizer.Sanitize(placeholderHtml);

        // Restore Liquid variables from placeholders
        sanitizedHtml = RestoreLiquidFromPlaceholders(sanitizedHtml, placeholderMap);

        return (sanitizedHtml, removedItems);
    }

    private HtmlSanitizer CreateSanitizer(List<string> removedItems)
    {
        var sanitizer = new HtmlSanitizer();

        // Clear defaults and set email-safe allowlists
        sanitizer.AllowedTags.Clear();
        foreach (var tag in EmailSafeTags)
            sanitizer.AllowedTags.Add(tag);
        foreach (var tag in sanitizerOptions.AdditionalAllowedTags)
            sanitizer.AllowedTags.Add(tag);

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in EmailSafeAttributes)
            sanitizer.AllowedAttributes.Add(attr);
        foreach (var attr in sanitizerOptions.AdditionalAllowedAttributes)
            sanitizer.AllowedAttributes.Add(attr);

        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in EmailSafeSchemes)
            sanitizer.AllowedSchemes.Add(scheme);
        foreach (var scheme in sanitizerOptions.AdditionalAllowedSchemes)
            sanitizer.AllowedSchemes.Add(scheme);

        // Track what gets removed
        sanitizer.RemovingTag += (_, e) => removedItems.Add($"tag:{e.Tag.TagName}");
        sanitizer.RemovingAttribute += (_, e) => removedItems.Add($"attr:{e.Tag.TagName}.{e.Attribute.Name}");
        sanitizer.RemovingStyle += (_, e) => removedItems.Add($"style:{e.Tag.TagName}.{e.Style.Name}");

        return sanitizer;
    }

    private static (string Html, Dictionary<string, string> PlaceholderMap) ReplaceLiquidWithPlaceholders(string html)
    {
        var placeholderMap = new Dictionary<string, string>();
        var regex = LiquidVariablePattern();

        var replaced = regex.Replace(html, match =>
        {
            var placeholder = $"LIQUID_{Guid.NewGuid():N}";
            placeholderMap[placeholder] = match.Value;
            return placeholder;
        });

        return (replaced, placeholderMap);
    }

    private static string RestoreLiquidFromPlaceholders(string html, Dictionary<string, string> placeholderMap)
    {
        foreach (var kvp in placeholderMap)
            html = html.Replace(kvp.Key, kvp.Value);

        return html;
    }
}
