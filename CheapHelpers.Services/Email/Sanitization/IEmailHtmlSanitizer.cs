namespace CheapHelpers.Services.Email.Sanitization;

/// <summary>
/// Validates and sanitizes user-authored HTML for safe inclusion in emails.
/// Handles Liquid template variable whitelisting and dangerous HTML stripping.
/// </summary>
public interface IEmailHtmlSanitizer
{
    /// <summary>
    /// Validates HTML without modifying it. Checks for unknown Liquid variables
    /// and performs a dry-run sanitization to detect what would be removed.
    /// <see cref="EmailSanitizationResult.SanitizedHtml"/> is not populated.
    /// </summary>
    EmailSanitizationResult Validate(string html);

    /// <summary>
    /// Validates and sanitizes HTML, returning the cleaned output.
    /// Strips dangerous tags, attributes, and styles while preserving email-safe formatting.
    /// Liquid template variables are preserved through placeholder substitution.
    /// </summary>
    EmailSanitizationResult Sanitize(string html);
}
