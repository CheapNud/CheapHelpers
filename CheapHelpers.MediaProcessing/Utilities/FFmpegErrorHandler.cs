using System.Diagnostics;
using CheapHelpers.MediaProcessing.Models;

namespace CheapHelpers.MediaProcessing.Utilities;

/// <summary>
/// Parses FFmpeg stderr output into structured errors with user-friendly messages and suggestions.
/// Recognizes common error patterns including file I/O, codec, NVENC/CUDA, memory, and disk issues.
/// </summary>
public static class FFmpegErrorHandler
{
    /// <summary>
    /// Parses FFmpeg error output and returns a structured <see cref="FFmpegError"/>
    /// with error classification, a user-friendly message, and a suggested resolution.
    /// </summary>
    /// <param name="errorOutput">The raw stderr output from FFmpeg.</param>
    /// <returns>A structured <see cref="FFmpegError"/> with classification and suggestion.</returns>
    public static FFmpegError ParseError(string errorOutput)
    {
        if (string.IsNullOrWhiteSpace(errorOutput))
        {
            return new FFmpegError
            {
                ErrorType = "Unknown",
                Message = "Unknown error occurred",
                Suggestion = "Check FFmpeg installation and input files"
            };
        }

        // File not found
        if (Contains(errorOutput, "no such file") || Contains(errorOutput, "does not exist"))
        {
            return new FFmpegError
            {
                ErrorType = "FileNotFound",
                Message = "Input file not found",
                Suggestion = "Verify the input file path exists and is accessible",
                RawError = errorOutput
            };
        }

        // Permission errors
        if (Contains(errorOutput, "permission denied") || Contains(errorOutput, "access denied"))
        {
            return new FFmpegError
            {
                ErrorType = "PermissionDenied",
                Message = "Permission denied accessing file",
                Suggestion = "Check file permissions or try running with administrator privileges",
                RawError = errorOutput
            };
        }

        // Corrupt file
        if (Contains(errorOutput, "invalid data") || Contains(errorOutput, "corrupt") ||
            Contains(errorOutput, "header missing") || Contains(errorOutput, "moov atom not found"))
        {
            return new FFmpegError
            {
                ErrorType = "CorruptFile",
                Message = "Input file appears to be corrupted or incomplete",
                Suggestion = "Try re-downloading or re-creating the input file. For partially downloaded files, ensure the download completed successfully.",
                RawError = errorOutput
            };
        }

        // Codec errors
        if (Contains(errorOutput, "codec not supported") || Contains(errorOutput, "unknown codec") ||
            Contains(errorOutput, "decoder not found") || Contains(errorOutput, "encoder not found"))
        {
            return new FFmpegError
            {
                ErrorType = "CodecNotSupported",
                Message = "Codec not supported by FFmpeg",
                Suggestion = "Install FFmpeg with full codec support or convert the file to a supported format (H.264/H.265)",
                RawError = errorOutput
            };
        }

        // NVENC / CUDA errors (check before generic OutOfMemory since NVENC OOM is more specific)
        if (Contains(errorOutput, "nvenc") || Contains(errorOutput, "cuda"))
        {
            if (Contains(errorOutput, "driver") || Contains(errorOutput, "not found"))
            {
                return new FFmpegError
                {
                    ErrorType = "NvencDriverError",
                    Message = "NVIDIA driver issue detected",
                    Suggestion = "Update NVIDIA drivers to the latest version. NVENC requires driver version 471.41 or newer.",
                    RawError = errorOutput
                };
            }

            if (Contains(errorOutput, "out of memory") || Contains(errorOutput, "vram"))
            {
                return new FFmpegError
                {
                    ErrorType = "NvencOutOfMemory",
                    Message = "GPU out of memory",
                    Suggestion = "Close other GPU-intensive applications or reduce video resolution.",
                    RawError = errorOutput
                };
            }

            return new FFmpegError
            {
                ErrorType = "NvencError",
                Message = "NVENC hardware acceleration error",
                Suggestion = "Try disabling hardware acceleration or update NVIDIA drivers",
                RawError = errorOutput
            };
        }

        // System out of memory
        if (Contains(errorOutput, "out of memory") || Contains(errorOutput, "cannot allocate memory"))
        {
            return new FFmpegError
            {
                ErrorType = "OutOfMemory",
                Message = "System out of memory",
                Suggestion = "Close other applications to free up RAM or process a shorter video segment",
                RawError = errorOutput
            };
        }

        // Disk space
        if (Contains(errorOutput, "no space left") || Contains(errorOutput, "disk full"))
        {
            return new FFmpegError
            {
                ErrorType = "DiskFull",
                Message = "Insufficient disk space",
                Suggestion = "Free up disk space on the output drive. Video processing requires significant temporary storage.",
                RawError = errorOutput
            };
        }

        // Frame rate
        if (Contains(errorOutput, "frame rate") || Contains(errorOutput, "invalid framerate"))
        {
            return new FFmpegError
            {
                ErrorType = "InvalidFrameRate",
                Message = "Invalid frame rate specified",
                Suggestion = "Verify the input video has a valid frame rate (common: 24, 30, 60 fps)",
                RawError = errorOutput
            };
        }

        // Resolution
        if (Contains(errorOutput, "resolution") || Contains(errorOutput, "invalid width") ||
            Contains(errorOutput, "invalid height"))
        {
            return new FFmpegError
            {
                ErrorType = "InvalidResolution",
                Message = "Invalid video resolution",
                Suggestion = "Verify the video resolution is valid (width and height must be positive even numbers)",
                RawError = errorOutput
            };
        }

        // Bitrate
        if (Contains(errorOutput, "bitrate") || Contains(errorOutput, "invalid bitrate"))
        {
            return new FFmpegError
            {
                ErrorType = "BitrateError",
                Message = "Invalid bitrate configuration",
                Suggestion = "Check the bitrate settings. Common values: 5M for 1080p, 10M for 4K.",
                RawError = errorOutput
            };
        }

        // Audio
        if (Contains(errorOutput, "audio stream") || Contains(errorOutput, "no audio"))
        {
            return new FFmpegError
            {
                ErrorType = "AudioError",
                Message = "Audio stream error",
                Suggestion = "The input file may not contain an audio stream, or the audio codec is unsupported.",
                RawError = errorOutput
            };
        }

        // Generic fallback
        Debug.WriteLine($"Unrecognized FFmpeg error: {errorOutput}");

        return new FFmpegError
        {
            ErrorType = "Unknown",
            Message = "FFmpeg processing error",
            Suggestion = "Check FFmpeg logs for details. The error output may contain specific information about what went wrong.",
            RawError = errorOutput
        };
    }

    /// <summary>
    /// Formats an <see cref="FFmpegError"/> into a user-friendly message with suggestion.
    /// </summary>
    /// <param name="ffmpegError">The parsed error to format.</param>
    /// <returns>A multi-line string with the error message and suggestion.</returns>
    public static string GetUserFriendlyMessage(FFmpegError ffmpegError) =>
        $"{ffmpegError.Message}\n\nSuggestion: {ffmpegError.Suggestion}";

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
