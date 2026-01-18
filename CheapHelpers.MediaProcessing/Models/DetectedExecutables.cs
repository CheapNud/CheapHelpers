namespace CheapHelpers.MediaProcessing.Services;

/// <summary>
/// Result of executable detection.
/// Cross-platform model used by both Windows and Linux detection services.
/// </summary>
public class DetectedExecutables
{
    public string? FFmpegPath { get; set; }
    public string? FFprobePath { get; set; }
    public string? MeltPath { get; set; }

    /// <summary>
    /// Additional detected executables (app-specific)
    /// </summary>
    public Dictionary<string, string?> AdditionalExecutables { get; set; } = new();

    public bool FFmpegFound => FFmpegPath != null;
    public bool FFprobeFound => FFprobePath != null;
    public bool MeltFound => MeltPath != null;
    public bool EssentialsFound => FFmpegFound && FFprobeFound;
}
