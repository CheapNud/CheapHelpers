namespace CheapHelpers.MediaProcessing.Models;

/// <summary>
/// Represents a parsed FFmpeg error with classification and a suggested resolution.
/// </summary>
public class FFmpegError
{
    /// <summary>
    /// Error classification (e.g., FileNotFound, PermissionDenied, NvencError, CodecNotSupported).
    /// </summary>
    public string ErrorType { get; set; } = "Unknown";

    /// <summary>
    /// User-friendly error message describing the problem.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Suggested solution or next steps for the user.
    /// </summary>
    public string Suggestion { get; set; } = "";

    /// <summary>
    /// Raw error output from FFmpeg stderr, when available.
    /// </summary>
    public string? RawError { get; set; }

    public override string ToString() =>
        $"[{ErrorType}] {Message}\nSuggestion: {Suggestion}";
}
