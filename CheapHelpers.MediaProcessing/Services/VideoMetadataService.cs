using System.Diagnostics;
using CheapHelpers.MediaProcessing.Models;
using FFMpegCore;

namespace CheapHelpers.MediaProcessing.Services;

/// <summary>
/// Extracts video metadata and generates thumbnails using FFProbe/FFMpeg.
/// Assumes FFMpegCore is already configured (via GlobalFFOptions.Configure) before use.
/// </summary>
public class VideoMetadataService
{
    /// <summary>
    /// Analyzes a video file and returns rich metadata.
    /// Returns null if the file doesn't exist, has no video stream, or analysis fails.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VideoMetadata"/> instance, or null on failure.</returns>
    public async Task<VideoMetadata?> GetVideoMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine($"Video file not found: {filePath}");
            return null;
        }

        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath, cancellationToken: cancellationToken);
            if (mediaInfo == null)
            {
                Debug.WriteLine($"FFProbe returned null for: {filePath}");
                return null;
            }

            var videoStream = mediaInfo.PrimaryVideoStream;
            if (videoStream == null)
            {
                Debug.WriteLine($"No video stream found in: {filePath}");
                return null;
            }

            var audioStream = mediaInfo.PrimaryAudioStream;
            var fileInfo = new FileInfo(filePath);

            return new VideoMetadata
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSizeBytes = fileInfo.Length,
                Duration = mediaInfo.Duration,
                Width = videoStream.Width,
                Height = videoStream.Height,
                FrameRate = videoStream.FrameRate,
                VideoCodec = videoStream.CodecName ?? "unknown",
                AudioCodec = audioStream?.CodecName,
                Format = mediaInfo.Format.FormatName ?? Path.GetExtension(filePath).TrimStart('.'),
                VideoBitrateKbps = videoStream.BitRate > 0 ? videoStream.BitRate / 1000.0 : null,
                AudioBitrateKbps = audioStream?.BitRate > 0 ? audioStream.BitRate / 1000.0 : null,
                PixelFormat = videoStream.PixelFormat
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error extracting video metadata: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validates that a video file exists, is non-empty, contains a video stream, and has a non-zero duration.
    /// Returns metadata on success, or a validation error message on failure.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (metadata, errorMessage). If valid, metadata is populated and errorMessage is null.</returns>
    public async Task<(VideoMetadata? Metadata, string? ErrorMessage)> ValidateVideoAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return (null, "Video file not found");

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
            return (null, "Video file is empty");

        var metadata = await GetVideoMetadataAsync(filePath, cancellationToken);
        if (metadata == null)
            return (null, "Failed to analyze video — no video stream found or FFProbe error");

        if (metadata.Duration == TimeSpan.Zero)
            return (null, "Video has zero duration");

        return (metadata, null);
    }

    /// <summary>
    /// Validates that an output video's duration matches the expected duration within a tolerance.
    /// </summary>
    /// <param name="filePath">Path to the output video file.</param>
    /// <param name="expectedDuration">The expected duration.</param>
    /// <param name="toleranceSeconds">Maximum allowed duration difference in seconds. Default is 2.0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (metadata, errorMessage). If valid, metadata is populated and errorMessage is null.</returns>
    public async Task<(VideoMetadata? Metadata, string? ErrorMessage)> ValidateOutputVideoAsync(
        string filePath,
        TimeSpan expectedDuration,
        double toleranceSeconds = 2.0,
        CancellationToken cancellationToken = default)
    {
        var (metadata, validationError) = await ValidateVideoAsync(filePath, cancellationToken);
        if (validationError != null)
            return (null, validationError);

        var durationDifference = Math.Abs((metadata!.Duration - expectedDuration).TotalSeconds);
        if (durationDifference > toleranceSeconds)
            return (metadata, $"Duration mismatch: expected {expectedDuration}, got {metadata.Duration} (difference: {durationDifference:F1}s)");

        return (metadata, null);
    }

    /// <summary>
    /// Generates a thumbnail image from a video file at the specified time offset.
    /// Defaults to 10% into the video (minimum 1 second).
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="outputPath">Path for the output thumbnail image.</param>
    /// <param name="timeOffset">Optional time offset for the thumbnail capture. Defaults to 10% of video duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the thumbnail was generated successfully.</returns>
    public async Task<bool> GenerateThumbnailAsync(
        string filePath,
        string outputPath,
        TimeSpan? timeOffset = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            Debug.WriteLine($"Video file not found: {filePath}");
            return false;
        }

        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath, cancellationToken: cancellationToken);
            if (mediaInfo == null) return false;

            var captureTime = timeOffset ?? TimeSpan.FromSeconds(
                Math.Max(1, mediaInfo.Duration.TotalSeconds * 0.1));

            // Clamp to video duration
            if (captureTime > mediaInfo.Duration)
                captureTime = TimeSpan.FromSeconds(mediaInfo.Duration.TotalSeconds / 2);

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            await FFMpeg.SnapshotAsync(filePath, outputPath, captureTime: captureTime, cancellationToken: cancellationToken);
            return File.Exists(outputPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating thumbnail: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Calculates the expected frame count for a video at a given frame rate.
    /// Uses Floor to match FFmpeg's extraction behavior.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="fps">Target frame rate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expected frame count, or 0 on failure.</returns>
    public async Task<int> CalculateExpectedFrameCountAsync(string filePath, double fps, CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath, cancellationToken: cancellationToken);
            if (mediaInfo == null) return 0;

            var calculatedFrames = (int)Math.Floor(mediaInfo.Duration.TotalSeconds * fps);
            Debug.WriteLine($"Calculated frame count: {calculatedFrames} (duration: {mediaInfo.Duration.TotalSeconds}s, fps: {fps})");
            return calculatedFrames;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error calculating frame count: {ex.Message}");
            return 0;
        }
    }
}
