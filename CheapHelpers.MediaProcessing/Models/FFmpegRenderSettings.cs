namespace CheapHelpers.MediaProcessing.Models;

/// <summary>
/// FFmpeg render settings for video encoding
/// Supports both hardware-accelerated (NVENC) and CPU encoding
/// </summary>
public class FFmpegRenderSettings
{
    /// <summary>
    /// Target frame rate
    /// </summary>
    public int FrameRate { get; set; } = 60;

    /// <summary>
    /// Whether to use hardware acceleration (NVENC)
    /// </summary>
    public bool UseHardwareAcceleration { get; set; }

    /// <summary>
    /// Video codec (e.g., "hevc_nvenc", "libx265", "h264_nvenc", "libx264")
    /// </summary>
    public string VideoCodec { get; set; } = "libx265";

    /// <summary>
    /// Audio codec (e.g., "aac", "libopus")
    /// </summary>
    public string AudioCodec { get; set; } = "aac";

    /// <summary>
    /// Audio bitrate (e.g., "128k", "192k", "256k")
    /// </summary>
    public string AudioBitrate { get; set; } = "128k";

    /// <summary>
    /// NVENC preset (p1=fastest, p7=best quality)
    /// Only used when UseHardwareAcceleration is true
    /// </summary>
    public string NvencPreset { get; set; } = "p7";

    /// <summary>
    /// CPU preset (ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow)
    /// Only used when UseHardwareAcceleration is false
    /// </summary>
    public string CpuPreset { get; set; } = "medium";

    /// <summary>
    /// Rate control mode (vbr, cbr, crf)
    /// </summary>
    public string RateControl { get; set; } = "vbr";

    /// <summary>
    /// Quality value (CQ for NVENC, CRF for CPU)
    /// Lower = better quality, higher file size
    /// Recommended: 18-23 for high quality, 24-28 for smaller files
    /// </summary>
    public int Quality { get; set; } = 23;

    /// <summary>
    /// Pixel format (e.g., "yuv420p", "yuv444p")
    /// </summary>
    public string PixelFormat { get; set; } = "yuv420p";

    /// <summary>
    /// Color space (e.g., "bt709", "bt2020nc")
    /// </summary>
    public string? ColorSpace { get; set; }

    /// <summary>
    /// Target bitrate in kbps (0 = use quality-based encoding)
    /// </summary>
    public int TargetBitrate { get; set; } = 0;

    /// <summary>
    /// Maximum bitrate in kbps (for VBR mode)
    /// </summary>
    public int MaxBitrate { get; set; } = 0;

    /// <summary>
    /// Buffer size in kbps (for rate control)
    /// </summary>
    public int BufferSize { get; set; } = 0;

    /// <summary>
    /// GPU device ID (for multi-GPU systems)
    /// </summary>
    public int GpuDeviceId { get; set; } = 0;

    /// <summary>
    /// Enable B-frames (better compression, slightly slower)
    /// </summary>
    public bool EnableBFrames { get; set; } = true;

    /// <summary>
    /// Number of B-frames (0-4, default 2)
    /// </summary>
    public int BFrameCount { get; set; } = 2;

    /// <summary>
    /// Get the quality parameter name based on codec
    /// </summary>
    public string GetQualityParameterName()
    {
        return VideoCodec.Contains("nvenc") ? "-cq" : "-crf";
    }

    /// <summary>
    /// Get the preset parameter based on codec
    /// </summary>
    public string GetPresetValue()
    {
        return UseHardwareAcceleration ? NvencPreset : CpuPreset;
    }

    /// <summary>
    /// Build FFmpeg arguments for encoding
    /// </summary>
    public string BuildEncodingArguments()
    {
        var args = new List<string>
        {
            $"-c:v {VideoCodec}"
        };

        if (UseHardwareAcceleration)
        {
            args.Add($"-preset {NvencPreset}");
            args.Add($"-cq {Quality}");
            args.Add($"-gpu {GpuDeviceId}");

            if (EnableBFrames && VideoCodec.Contains("hevc"))
            {
                args.Add($"-bf {BFrameCount}");
            }
        }
        else
        {
            args.Add($"-preset {CpuPreset}");
            args.Add($"-crf {Quality}");
        }

        args.Add($"-pix_fmt {PixelFormat}");

        if (!string.IsNullOrEmpty(ColorSpace))
        {
            args.Add($"-colorspace {ColorSpace}");
        }

        args.Add($"-c:a {AudioCodec}");
        args.Add($"-b:a {AudioBitrate}");

        return string.Join(" ", args);
    }
}
