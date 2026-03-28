namespace CheapHelpers.Services.Email.Sanitization;

/// <summary>
/// Configuration options for the email HTML sanitizer.
/// </summary>
public class EmailHtmlSanitizerOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "EmailHtmlSanitizer";

    /// <summary>
    /// Whitelist of allowed Liquid variable names (e.g., "Data.FirstName", "Theme.Primary").
    /// When non-empty, any Liquid variable not in this set will be flagged during validation.
    /// When empty, Liquid variable validation is skipped entirely.
    /// </summary>
    public HashSet<string> AllowedLiquidVariables { get; set; } = [];

    /// <summary>
    /// Additional HTML tags to allow beyond the email-safe defaults.
    /// </summary>
    public HashSet<string> AdditionalAllowedTags { get; set; } = [];

    /// <summary>
    /// Additional HTML attributes to allow beyond the email-safe defaults.
    /// </summary>
    public HashSet<string> AdditionalAllowedAttributes { get; set; } = [];

    /// <summary>
    /// Additional URI schemes to allow beyond the defaults (http, https, mailto, data).
    /// </summary>
    public HashSet<string> AdditionalAllowedSchemes { get; set; } = [];
}
