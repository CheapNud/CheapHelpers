using CheapHelpers.Extensions;

namespace CheapHelpers.MediaProcessing.Models;

/// <summary>
/// Rich video file metadata extracted via FFProbe.
/// Includes both raw properties and computed display strings.
/// </summary>
public class VideoMetadata
{
    /// <summary>Full file path.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>File name without directory path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Video duration.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Video width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Video height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Frame rate (frames per second).</summary>
    public double FrameRate { get; set; }

    /// <summary>Video codec name (e.g., h264, hevc, vp9).</summary>
    public string VideoCodec { get; set; } = string.Empty;

    /// <summary>Audio codec name (e.g., aac, mp3, opus), or null if no audio.</summary>
    public string? AudioCodec { get; set; }

    /// <summary>Container format (e.g., mp4, mkv, avi).</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Video bitrate in kbps, or null if unavailable.</summary>
    public double? VideoBitrateKbps { get; set; }

    /// <summary>Audio bitrate in kbps, or null if unavailable.</summary>
    public double? AudioBitrateKbps { get; set; }

    /// <summary>Pixel format (e.g., yuv420p, yuv444p).</summary>
    public string? PixelFormat { get; set; }

    /// <summary>Whether the file contains an audio stream.</summary>
    public bool HasAudio => !string.IsNullOrEmpty(AudioCodec);

    /// <summary>Estimated total frame count based on duration and frame rate.</summary>
    public int TotalFrames => (int)(Duration.TotalSeconds * FrameRate);

    /// <summary>Resolution string (e.g., "1920x1080").</summary>
    public string Resolution => $"{Width}x{Height}";

    /// <summary>
    /// Human-readable resolution category (e.g., "1080p FHD", "4K UHD").
    /// </summary>
    public string ResolutionLabel => Height switch
    {
        >= 2160 => "4K UHD",
        >= 1440 => "1440p QHD",
        >= 1080 => "1080p FHD",
        >= 720 => "720p HD",
        >= 576 => "576p SD",
        >= 480 => "480p",
        _ => $"{Height}p"
    };

    /// <summary>Formatted duration (e.g., "1:23:45" or "23:45").</summary>
    public string DurationFormatted => Duration.TotalHours >= 1
        ? $"{(int)Duration.TotalHours}:{Duration.Minutes:D2}:{Duration.Seconds:D2}"
        : $"{Duration.Minutes}:{Duration.Seconds:D2}";

    /// <summary>Formatted file size (e.g., "1.50 GB").</summary>
    public string FileSizeFormatted => FileSizeBytes.ToReadableFileSize();

    /// <summary>Formatted frame rate (e.g., "23.976 fps" or "60 fps").</summary>
    public string FrameRateFormatted => FrameRate % 1 == 0
        ? $"{(int)FrameRate} fps"
        : $"{FrameRate:F3} fps";
}
