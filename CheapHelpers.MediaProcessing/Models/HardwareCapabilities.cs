namespace CheapHelpers.MediaProcessing.Models;

/// <summary>
/// Detected hardware capabilities for video processing optimization
/// </summary>
public class HardwareCapabilities
{
    /// <summary>
    /// Number of logical CPU cores
    /// </summary>
    public int CpuCoreCount { get; set; }

    /// <summary>
    /// CPU name/model
    /// </summary>
    public string CpuName { get; set; } = "Unknown";

    /// <summary>
    /// Whether the CPU is Intel (for Quick Sync support)
    /// </summary>
    public bool IsIntelCpu { get; set; }

    /// <summary>
    /// Whether an NVIDIA GPU was detected
    /// </summary>
    public bool HasNvidiaGpu { get; set; }

    /// <summary>
    /// Primary GPU name/model
    /// </summary>
    public string GpuName { get; set; } = "Unknown";

    /// <summary>
    /// Whether NVENC hardware encoding is available
    /// </summary>
    public bool NvencAvailable { get; set; }

    /// <summary>
    /// All detected GPUs (for multi-GPU systems)
    /// </summary>
    public List<string> AvailableGpus { get; set; } = [];

    /// <summary>
    /// Available hardware encoders with their capabilities
    /// </summary>
    public Dictionary<string, HardwareEncoderInfo> SupportedEncoders { get; set; } = new();

    /// <summary>
    /// Whether hardware-accelerated FFmpeg encoding should be used
    /// </summary>
    public bool ShouldUseFFmpegNvenc => NvencAvailable;

    /// <summary>
    /// Estimated speedup when using NVENC vs CPU for FFmpeg
    /// Based on typical benchmarks: 500fps vs 30-60fps
    /// </summary>
    public double NvencSpeedupFactor => NvencAvailable ? 8.5 : 1.0;

    /// <summary>
    /// Estimate render time for given duration
    /// </summary>
    public TimeSpan EstimateFFmpegRenderTime(TimeSpan videoDuration, bool useNvenc)
    {
        // Baseline: CPU encoding is roughly 2:1 (1 hour video = 2 hours to encode) with medium preset
        var baselineMultiplier = 2.0;

        if (useNvenc && NvencAvailable)
        {
            // NVENC is 8-10x faster
            baselineMultiplier /= NvencSpeedupFactor;
        }

        return TimeSpan.FromTicks((long)(videoDuration.Ticks * baselineMultiplier));
    }

    /// <summary>
    /// Get human-readable time savings description
    /// </summary>
    public string GetTimeSavingsDescription(TimeSpan videoDuration)
    {
        if (!NvencAvailable)
            return "No NVIDIA GPU - using CPU encoding";

        var cpuTime = EstimateFFmpegRenderTime(videoDuration, useNvenc: false);
        var nvencTime = EstimateFFmpegRenderTime(videoDuration, useNvenc: true);
        var savings = cpuTime - nvencTime;

        return $"NVENC saves ~{savings.TotalMinutes:F0} minutes (CPU: {cpuTime.TotalMinutes:F0}m vs NVENC: {nvencTime.TotalMinutes:F0}m)";
    }

    /// <summary>
    /// Get the best available encoder for a codec type
    /// </summary>
    public HardwareEncoderInfo? GetBestEncoder(string codecType)
    {
        var preferredOrder = new[] { "nvenc", "qsv", "amf" };

        foreach (var vendor in preferredOrder)
        {
            var encoderKey = $"{codecType}_{vendor}";
            if (SupportedEncoders.TryGetValue(encoderKey, out var encoder) && encoder.IsAvailable)
            {
                return encoder;
            }
        }

        return null;
    }
}

/// <summary>
/// Information about a hardware encoder
/// </summary>
public class HardwareEncoderInfo
{
    /// <summary>
    /// FFmpeg codec name (e.g., "hevc_nvenc")
    /// </summary>
    public string CodecName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this encoder is available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Hardware vendor type (NVENC, QSV, AMF)
    /// </summary>
    public string VendorType { get; set; } = string.Empty;

    /// <summary>
    /// Estimated speedup factor compared to CPU encoding
    /// </summary>
    public double EstimatedSpeedupFactor { get; set; } = 1.0;

    /// <summary>
    /// Description of encoder characteristics
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
