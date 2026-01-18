using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using CheapHelpers.MediaProcessing.Models;

namespace CheapHelpers.MediaProcessing.Services.Linux;

/// <summary>
/// Linux implementation for detecting hardware capabilities.
/// Uses nvidia-smi for GPU detection and /proc/ for CPU info.
/// </summary>
[UnsupportedOSPlatform("windows")]
public partial class LinuxHardwareDetectionService(LinuxExecutableDetectionService executableDetection)
{
    // Encoding quality constants
    private const int NVENC_OPTIMAL_QUALITY = 19;
    private const int NVENC_HIGH_QUALITY = 18;
    private const int CPU_DEFAULT_QUALITY = 23;
    private const int CPU_HIGH_QUALITY = 20;
    private const int CPU_FAST_QUALITY = 26;

    // NVENC preset constants
    private const string NVENC_BEST_PRESET = "p7";
    private const string NVENC_FAST_PRESET = "p4";

    // CPU preset constants
    private const string CPU_DEFAULT_PRESET = "medium";
    private const string CPU_HIGH_QUALITY_PRESET = "slow";
    private const string CPU_FAST_PRESET = "fast";

    private HardwareCapabilities? _cachedCapabilities;
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

    /// <summary>
    /// Detect hardware capabilities (cached after first call)
    /// </summary>
    public virtual async Task<HardwareCapabilities> DetectHardwareAsync()
    {
        // Fast path - return cached value
        if (_cachedCapabilities != null)
            return _cachedCapabilities;

        await _cacheSemaphore.WaitAsync();
        try
        {
            if (_cachedCapabilities != null)
                return _cachedCapabilities;

            var capabilities = new HardwareCapabilities
            {
                CpuCoreCount = Environment.ProcessorCount,
                CpuName = GetCpuName(),
                HasNvidiaGpu = await DetectNvidiaGpuAsync(),
                GpuName = await GetGpuNameAsync(),
                NvencAvailable = await IsNvencAvailableAsync(),
                AvailableGpus = await GetAllGpuNamesAsync(),
                IsIntelCpu = IsIntelCpu()
            };

            // Detect hardware encoders
            await DetectHardwareEncodersAsync(capabilities);

            Debug.WriteLine("=== Linux Hardware Detection ===");
            Debug.WriteLine($"CPU: {capabilities.CpuName} ({capabilities.CpuCoreCount} cores)");
            Debug.WriteLine($"GPU: {capabilities.GpuName}");
            Debug.WriteLine($"NVIDIA GPU: {capabilities.HasNvidiaGpu}");
            Debug.WriteLine($"NVENC Available: {capabilities.NvencAvailable}");
            Debug.WriteLine($"Hardware Encoders Detected: {capabilities.SupportedEncoders.Count(e => e.Value.IsAvailable)}");
            Debug.WriteLine("================================");

            _cachedCapabilities = capabilities;
            return capabilities;
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <summary>
    /// Get optimal FFmpeg settings for video encoding
    /// </summary>
    public virtual async Task<FFmpegRenderSettings> GetOptimalFFmpegSettingsAsync(int targetFps = 60)
    {
        var hw = await DetectHardwareAsync();

        var settings = new FFmpegRenderSettings
        {
            FrameRate = targetFps,
            UseHardwareAcceleration = hw.NvencAvailable
        };

        if (hw.NvencAvailable)
        {
            settings.VideoCodec = "hevc_nvenc";
            settings.NvencPreset = NVENC_BEST_PRESET;
            settings.RateControl = "vbr";
            settings.Quality = NVENC_OPTIMAL_QUALITY;
        }
        else
        {
            settings.VideoCodec = "libx265";
            settings.CpuPreset = CPU_DEFAULT_PRESET;
            settings.Quality = CPU_DEFAULT_QUALITY;
        }

        var speedDescription = hw.NvencAvailable
            ? "NVENC hardware acceleration"
            : "CPU encoding (no NVIDIA GPU detected)";

        Debug.WriteLine($"Optimal FFmpeg settings: {speedDescription}");

        return settings;
    }

    /// <summary>
    /// Get high-quality FFmpeg settings
    /// </summary>
    public virtual async Task<FFmpegRenderSettings> GetHighQualityFFmpegSettingsAsync(int targetFps = 60)
    {
        var settings = await GetOptimalFFmpegSettingsAsync(targetFps);

        if (settings.UseHardwareAcceleration)
        {
            settings.Quality = NVENC_HIGH_QUALITY;
            settings.NvencPreset = NVENC_BEST_PRESET;
        }
        else
        {
            settings.CpuPreset = CPU_HIGH_QUALITY_PRESET;
            settings.Quality = CPU_HIGH_QUALITY;
        }

        return settings;
    }

    /// <summary>
    /// Get fast FFmpeg settings
    /// </summary>
    public virtual async Task<FFmpegRenderSettings> GetFastFFmpegSettingsAsync(int targetFps = 60)
    {
        var settings = await GetOptimalFFmpegSettingsAsync(targetFps);

        if (settings.UseHardwareAcceleration)
        {
            settings.NvencPreset = NVENC_FAST_PRESET;
            settings.Quality = CPU_DEFAULT_QUALITY;
        }
        else
        {
            settings.CpuPreset = CPU_FAST_PRESET;
            settings.Quality = CPU_FAST_QUALITY;
        }

        return settings;
    }

    /// <summary>
    /// Get the best available hardware encoder
    /// </summary>
    public async Task<HardwareEncoderInfo?> GetBestEncoderAsync(string codecType = "hevc")
    {
        var hw = await DetectHardwareAsync();

        // Priority: NVENC > VAAPI > Software
        var preferredOrder = new[] { "nvenc", "vaapi" };

        foreach (var vendor in preferredOrder)
        {
            var encoderKey = $"{codecType}_{vendor}";
            if (hw.SupportedEncoders.TryGetValue(encoderKey, out var encoder) && encoder.IsAvailable)
            {
                return encoder;
            }
        }

        return null;
    }

    private async Task<bool> DetectNvidiaGpuAsync()
    {
        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetGpuNameAsync()
    {
        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            }
        }
        catch { }

        // Fallback: try lspci
        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "lspci",
                    Arguments = "-v",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var vgaMatch = VgaControllerRegex().Match(output);
                if (vgaMatch.Success)
                {
                    return vgaMatch.Groups[1].Value.Trim();
                }
            }
        }
        catch { }

        return "Unknown GPU";
    }

    private async Task<List<string>> GetAllGpuNamesAsync()
    {
        var gpuList = new List<string>();

        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var gpuNames = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                gpuList.AddRange(gpuNames.Select(n => n.Trim()));
            }
        }
        catch { }

        if (gpuList.Count == 0)
        {
            gpuList.Add("Unknown GPU");
        }

        return gpuList;
    }

    private string GetCpuName()
    {
        try
        {
            if (File.Exists("/proc/cpuinfo"))
            {
                var cpuInfo = File.ReadAllText("/proc/cpuinfo");
                var match = CpuModelNameRegex().Match(cpuInfo);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        catch { }

        return "Unknown CPU";
    }

    private bool IsIntelCpu()
    {
        var cpuName = GetCpuName();
        return cpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase);
    }

    private async Task DetectHardwareEncodersAsync(HardwareCapabilities capabilities)
    {
        var encodersToCheck = new Dictionary<string, HardwareEncoderInfo>
        {
            ["av1_nvenc"] = new()
            {
                CodecName = "av1_nvenc",
                DisplayName = "AV1 (NVENC)",
                VendorType = "NVENC",
                EstimatedSpeedupFactor = 12.0,
                Description = "Highest compression efficiency with NVIDIA hardware"
            },
            ["h264_nvenc"] = new()
            {
                CodecName = "h264_nvenc",
                DisplayName = "H.264 (NVENC)",
                VendorType = "NVENC",
                EstimatedSpeedupFactor = 8.0,
                Description = "Most compatible format on NVIDIA GPUs"
            },
            ["hevc_nvenc"] = new()
            {
                CodecName = "hevc_nvenc",
                DisplayName = "HEVC/H.265 (NVENC)",
                VendorType = "NVENC",
                EstimatedSpeedupFactor = 8.5,
                Description = "Excellent quality-to-size ratio on NVIDIA GPUs"
            },
            ["h264_vaapi"] = new()
            {
                CodecName = "h264_vaapi",
                DisplayName = "H.264 (VAAPI)",
                VendorType = "VAAPI",
                EstimatedSpeedupFactor = 6.0,
                Description = "Linux video acceleration API"
            },
            ["hevc_vaapi"] = new()
            {
                CodecName = "hevc_vaapi",
                DisplayName = "HEVC/H.265 (VAAPI)",
                VendorType = "VAAPI",
                EstimatedSpeedupFactor = 6.5,
                Description = "Linux video acceleration API"
            }
        };

        var ffmpegPath = executableDetection.DetectFFmpeg(useSvpEncoders: false, customPath: null);
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            capabilities.SupportedEncoders = encodersToCheck;
            return;
        }

        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-encoders",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                foreach (var encoder in encodersToCheck)
                {
                    encoder.Value.IsAvailable = output.Contains(encoder.Key);
                    Debug.WriteLine($"Hardware encoder {encoder.Key}: {(encoder.Value.IsAvailable ? "Available" : "Not available")}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting hardware encoders: {ex.Message}");
        }

        capabilities.SupportedEncoders = encodersToCheck;
    }

    private async Task<bool> IsNvencAvailableAsync()
    {
        var ffmpegPath = executableDetection.DetectFFmpeg(useSvpEncoders: false, customPath: null);
        if (string.IsNullOrEmpty(ffmpegPath))
            return false;

        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-encoders",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return output.Contains("h264_nvenc") || output.Contains("hevc_nvenc");
            }
        }
        catch { }

        return false;
    }

    [GeneratedRegex(@"VGA compatible controller:\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex VgaControllerRegex();

    [GeneratedRegex(@"model name\s*:\s*(.+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CpuModelNameRegex();
}
