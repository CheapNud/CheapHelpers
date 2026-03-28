namespace CheapHelpers.Services.Email.Sanitization;

/// <summary>
/// Result of an email HTML validation or sanitization operation.
/// </summary>
public sealed record EmailSanitizationResult
{
    /// <summary>
    /// Whether the HTML passed validation (no unknown Liquid variables and no dangerous content).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The sanitized HTML output. Empty when only <see cref="IEmailHtmlSanitizer.Validate"/> is called.
    /// </summary>
    public string SanitizedHtml { get; init; } = string.Empty;

    /// <summary>
    /// HTML elements, attributes, or styles that were removed during sanitization.
    /// </summary>
    public IReadOnlyList<string> RemovedItems { get; init; } = [];

    /// <summary>
    /// Liquid variables found in the HTML that are not in the allowed whitelist.
    /// Empty when <see cref="EmailHtmlSanitizerOptions.AllowedLiquidVariables"/> is not configured.
    /// </summary>
    public IReadOnlyList<string> UnknownLiquidVariables { get; init; } = [];
}
