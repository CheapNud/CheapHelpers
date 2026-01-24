using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CheapHelpers.MediaProcessing.Models;

namespace CheapHelpers.MediaProcessing.Services;

/// <summary>
/// Detects hardware capabilities including CPU, GPU, and available video encoders.
/// Provides information for optimizing video processing operations.
/// </summary>
/// <remarks>
/// PLATFORM REQUIREMENT: This service requires Windows operating system.
/// Uses Windows Management Instrumentation (WMI) for hardware detection.
/// Attempting to use this service on non-Windows platforms will throw <see cref="PlatformNotSupportedException"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
public class HardwareDetectionService(SvpDetectionService svpDetection)
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

    // TODO: Consider refactoring to AsyncLazy<T> for cleaner caching pattern
    private volatile HardwareCapabilities? _cachedCapabilities;
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

    /// <summary>
    /// Detect hardware capabilities (cached after first call)
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when running on non-Windows platform</exception>
    public virtual async Task<HardwareCapabilities> DetectHardwareAsync()
    {
        ThrowIfNotWindows();

        // Fast path - return cached value without lock contention (volatile ensures visibility)
        if (_cachedCapabilities != null)
            return _cachedCapabilities;

        await _cacheSemaphore.WaitAsync();
        try
        {
            // Double-check inside lock prevents multiple detections when concurrent calls
            // arrive before cache is populated - this is the standard double-check locking pattern
            if (_cachedCapabilities != null)
                return _cachedCapabilities;

            var capabilities = new HardwareCapabilities
            {
                CpuCoreCount = Environment.ProcessorCount,
                CpuName = GetCpuName(),
                HasNvidiaGpu = await DetectNvidiaGpuAsync(),
                GpuName = GetGpuName(),
                NvencAvailable = await IsNvencAvailableAsync(),
                AvailableGpus = GetAllGpuNames(),
                IsIntelCpu = IsIntelCpu()
            };

            // Detect individual hardware encoders
            await DetectHardwareEncodersAsync(capabilities);

            Debug.WriteLine("=== Hardware Detection ===");
            Debug.WriteLine($"CPU: {capabilities.CpuName} ({capabilities.CpuCoreCount} cores)");
            Debug.WriteLine($"GPU: {capabilities.GpuName}");
            Debug.WriteLine($"NVIDIA GPU: {capabilities.HasNvidiaGpu}");
            Debug.WriteLine($"NVENC Available: {capabilities.NvencAvailable}");
            Debug.WriteLine($"Hardware Encoders Detected: {capabilities.SupportedEncoders.Count(e => e.Value.IsAvailable)}");
            Debug.WriteLine("========================");

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
    /// Uses NVENC if available (8-10x faster than CPU)
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
            // NVENC settings - maximum quality at high speed
            settings.VideoCodec = "hevc_nvenc";
            settings.NvencPreset = NVENC_BEST_PRESET;
            settings.RateControl = "vbr";
            settings.Quality = NVENC_OPTIMAL_QUALITY;
        }
        else
        {
            // CPU fallback
            settings.VideoCodec = "libx265";
            settings.CpuPreset = CPU_DEFAULT_PRESET;
            settings.Quality = CPU_DEFAULT_QUALITY;
        }

        var speedDescription = hw.NvencAvailable
            ? "NVENC hardware acceleration (8-10x faster than CPU!)"
            : "CPU encoding (no NVIDIA GPU detected)";

        Debug.WriteLine($"Optimal FFmpeg settings: {speedDescription}");

        return settings;
    }

    /// <summary>
    /// Get high-quality FFmpeg settings (slower, better compression)
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
    /// Get fast FFmpeg settings (draft quality, maximum speed)
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
    /// Get the best available hardware encoder for a given codec type
    /// </summary>
    public async Task<HardwareEncoderInfo?> GetBestEncoderAsync(string codecType = "hevc")
    {
        var hw = await DetectHardwareAsync();

        // Priority: NVENC > QSV > AMF > Software
        var preferredOrder = new[] { "nvenc", "qsv", "amf" };

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
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            // Fallback: check if nvidia-smi exists
            try
            {
                using var process = SysProcess.Start(new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name --format=csv,noheader",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch { }

            return false;
        }
    }

    private string GetGpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var results = searcher.Get();

            string? firstGpuName = null;

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var name = obj["Name"]?.ToString() ?? "";

                    // Prefer NVIDIA GPU name
                    if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }

                    // Remember first GPU as fallback
                    firstGpuName ??= name;
                }
            }

            return firstGpuName ?? "Unknown GPU";
        }
        catch
        {
            return "Unknown GPU";
        }
    }

    private List<string> GetAllGpuNames()
    {
        var gpuList = new List<string>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        gpuList.Add(name);
                    }
                }
            }

            if (gpuList.Count == 0)
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
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        var gpuNames = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                        gpuList.AddRange(gpuNames);
                    }
                }
                catch { }
            }

            if (gpuList.Count == 0)
            {
                gpuList.Add("Unknown GPU");
            }

            return gpuList;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting GPUs: {ex.Message}");
            return ["Unknown GPU"];
        }
    }

    private string GetCpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    return obj["Name"]?.ToString() ?? "Unknown CPU";
                }
            }

            return "Unknown CPU";
        }
        catch
        {
            return "Unknown CPU";
        }
    }

    private bool IsIntelCpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var cpuName = obj["Name"]?.ToString() ?? "";
                    var manufacturer = obj["Manufacturer"]?.ToString() ?? "";

                    var isIntel = manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                                 cpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase);

                    return isIntel;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting CPU vendor: {ex.Message}");
            return false;
        }
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
                Description = "Highest compression efficiency, best for archival and streaming"
            },
            ["av1_qsv"] = new()
            {
                CodecName = "av1_qsv",
                DisplayName = "AV1 (Intel Quick Sync)",
                VendorType = "QSV",
                EstimatedSpeedupFactor = 8.0,
                Description = "Good compression efficiency with Intel integrated graphics"
            },
            ["h264_amf"] = new()
            {
                CodecName = "h264_amf",
                DisplayName = "H.264 (AMD AMF)",
                VendorType = "AMF",
                EstimatedSpeedupFactor = 7.0,
                Description = "Universal compatibility with AMD hardware acceleration"
            },
            ["h264_nvenc"] = new()
            {
                CodecName = "h264_nvenc",
                DisplayName = "H.264 (NVENC)",
                VendorType = "NVENC",
                EstimatedSpeedupFactor = 8.0,
                Description = "Most compatible format, excellent speed on NVIDIA GPUs"
            },
            ["h264_qsv"] = new()
            {
                CodecName = "h264_qsv",
                DisplayName = "H.264 (Intel Quick Sync)",
                VendorType = "QSV",
                EstimatedSpeedupFactor = 6.0,
                Description = "Good compatibility with Intel integrated graphics"
            },
            ["hevc_amf"] = new()
            {
                CodecName = "hevc_amf",
                DisplayName = "HEVC/H.265 (AMD AMF)",
                VendorType = "AMF",
                EstimatedSpeedupFactor = 7.5,
                Description = "Better compression than H.264 with AMD hardware"
            },
            ["hevc_nvenc"] = new()
            {
                CodecName = "hevc_nvenc",
                DisplayName = "HEVC/H.265 (NVENC)",
                VendorType = "NVENC",
                EstimatedSpeedupFactor = 8.5,
                Description = "Excellent quality-to-size ratio on NVIDIA GPUs"
            },
            ["hevc_qsv"] = new()
            {
                CodecName = "hevc_qsv",
                DisplayName = "HEVC/H.265 (Intel Quick Sync)",
                VendorType = "QSV",
                EstimatedSpeedupFactor = 6.5,
                Description = "Good compression with Intel integrated graphics"
            },
            ["vp9_qsv"] = new()
            {
                CodecName = "vp9_qsv",
                DisplayName = "VP9 (Intel Quick Sync)",
                VendorType = "QSV",
                EstimatedSpeedupFactor = 5.0,
                Description = "Open format optimized for web streaming"
            }
        };

        var ffmpegPath = await GetFFmpegPathAsync();
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            capabilities.SupportedEncoders = encodersToCheck;
            return;
        }

        var isIntelCpu = capabilities.IsIntelCpu;

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
                    var foundInFfmpeg = output.Contains(encoder.Key);

                    if (encoder.Value.VendorType == "QSV")
                    {
                        if (!isIntelCpu)
                        {
                            encoder.Value.IsAvailable = false;
                            Debug.WriteLine($"Hardware encoder {encoder.Key}: Disabled (Intel CPU required)");
                        }
                        else
                        {
                            encoder.Value.IsAvailable = foundInFfmpeg;
                            Debug.WriteLine($"Hardware encoder {encoder.Key}: {(encoder.Value.IsAvailable ? "Available" : "Not available")}");
                        }
                    }
                    else
                    {
                        encoder.Value.IsAvailable = foundInFfmpeg;
                        Debug.WriteLine($"Hardware encoder {encoder.Key}: {(encoder.Value.IsAvailable ? "Available" : "Not available")}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error detecting hardware encoders: {ex.Message}");
        }

        capabilities.SupportedEncoders = encodersToCheck;
    }

    private async Task<string?> GetFFmpegPathAsync()
    {
        var ffmpegPaths = new List<string>();

        var svp = svpDetection.DetectSvpInstallation();
        if (svp.IsInstalled && File.Exists(svp.FFmpegPath))
        {
            ffmpegPaths.Add(svp.FFmpegPath);
        }

        ffmpegPaths.Add("ffmpeg");
        ffmpegPaths.Add(@"C:\Program Files\Shotcut\ffmpeg.exe");
        ffmpegPaths.Add(@"C:\Program Files (x86)\Shotcut\ffmpeg.exe");

        foreach (var ffmpegPath in ffmpegPaths)
        {
            try
            {
                using var process = new SysProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return ffmpegPath;
                }
            }
            catch { }
        }

        return null;
    }

    private async Task<bool> IsNvencAvailableAsync()
    {
        var ffmpegPaths = new List<string>();

        var svp = svpDetection.DetectSvpInstallation();
        if (svp.IsInstalled && File.Exists(svp.FFmpegPath))
        {
            ffmpegPaths.Add(svp.FFmpegPath);
        }

        ffmpegPaths.Add("ffmpeg");
        ffmpegPaths.Add(@"C:\Program Files\Shotcut\ffmpeg.exe");
        ffmpegPaths.Add(@"C:\Program Files (x86)\Shotcut\ffmpeg.exe");

        foreach (var ffmpegPath in ffmpegPaths)
        {
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
                    var hasH264Nvenc = output.Contains("h264_nvenc");
                    var hasHevcNvenc = output.Contains("hevc_nvenc");

                    if (hasH264Nvenc || hasHevcNvenc)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch { }
        }

        return false;
    }

    /// <summary>
    /// Throws PlatformNotSupportedException if not running on Windows
    /// </summary>
    private static void ThrowIfNotWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "HardwareDetectionService requires Windows operating system. " +
                "This service uses Windows Management Instrumentation (WMI) which is not available on other platforms.");
        }
    }
}
