using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Xml.Linq;

namespace CheapHelpers.MediaProcessing.Services;

/// <summary>
/// Detects SmoothVideo Project (SVP) 4 Pro installation and provides paths to encoders.
/// SVP includes high-quality FFmpeg builds with full hardware acceleration support.
/// </summary>
/// <remarks>
/// PLATFORM REQUIREMENT: This service requires Windows operating system.
/// Uses Windows-specific paths and the 'where' command for executable detection.
/// PREFER SVP's FFmpeg over other installations for better NVENC performance.
/// Detection priority: SVP installation > Custom path > System PATH > Common locations.
/// </remarks>
[SupportedOSPlatform("windows")]
public class SvpDetectionService
{
    private const string SVP_INSTALL_PATH_X86 = @"C:\Program Files (x86)\SVP 4";
    private const string SVP_INSTALL_PATH_X64 = @"C:\Program Files\SVP 4";
    private const int PROCESS_TIMEOUT_MS = 1000;

    /// <summary>
    /// Characters that are not allowed in file paths for security
    /// </summary>
    private static readonly char[] InvalidPathChars = ['&', '|', ';', '$', '`', '"', '\'', '<', '>', '(', ')', '{', '}', '[', ']', '\n', '\r'];

    private SvpInstallation? _cachedInstallation;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Validates a file path to prevent command injection
    /// </summary>
    private static bool IsValidPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Check for command injection characters
        if (path.IndexOfAny(InvalidPathChars) >= 0)
            return false;

        // Check for path traversal outside of normal directory navigation
        // Allow single ".." for relative paths but prevent injection patterns
        if (path.Contains("...") || path.Contains("..\\..\\..\\.."))
            return false;

        return true;
    }

    /// <summary>
    /// Detect SVP installation (cached after first call)
    /// </summary>
    public virtual SvpInstallation DetectSvpInstallation()
    {
        // Fast path - return cached value without locking
        if (_cachedInstallation != null)
            return _cachedInstallation;

        // Thread-safe initialization - entire detection inside lock
        lock (_cacheLock)
        {
            // Double-check after acquiring lock
            if (_cachedInstallation != null)
                return _cachedInstallation;

            var installation = new SvpInstallation();

            // Check common installation paths
            if (Directory.Exists(SVP_INSTALL_PATH_X86))
            {
                installation.IsInstalled = true;
                installation.InstallPath = SVP_INSTALL_PATH_X86;
            }
            else if (Directory.Exists(SVP_INSTALL_PATH_X64))
            {
                installation.IsInstalled = true;
                installation.InstallPath = SVP_INSTALL_PATH_X64;
            }

            if (installation.IsInstalled)
            {
                PopulateSvpPaths(installation);
                PopulateSvpVersion(installation);

                Debug.WriteLine("=== SVP 4 Pro Detection ===");
                Debug.WriteLine($"Installed: {installation.IsInstalled}");
                Debug.WriteLine($"Path: {installation.InstallPath}");
                Debug.WriteLine($"Version: {installation.Version}");
                Debug.WriteLine($"FFmpeg: {installation.FFmpegPath}");
                Debug.WriteLine($"Python: {installation.PythonPath}");
                Debug.WriteLine($"FFmpeg has NVENC: {installation.FFmpegHasNvenc}");
                Debug.WriteLine("===========================");
            }
            else
            {
                Debug.WriteLine("SVP 4 Pro not detected");
            }

            _cachedInstallation = installation;
            return installation;
        }
    }

    /// <summary>
    /// Get FFmpeg path with priority order:
    /// 1. SVP installation (if enabled in settings)
    /// 2. Custom path
    /// 3. System PATH
    /// 4. Common installation locations
    /// </summary>
    /// <param name="useSvpEncoders">Whether to prefer SVP's FFmpeg installation</param>
    /// <param name="customPath">Optional custom path to FFmpeg executable</param>
    /// <returns>Path to FFmpeg executable, or null if not found</returns>
    public string? GetPreferredFFmpegPath(bool useSvpEncoders, string? customPath = null)
    {
        // 1. SVP installation (if enabled)
        if (useSvpEncoders)
        {
            var svp = DetectSvpInstallation();
            if (svp.IsInstalled && File.Exists(svp.FFmpegPath))
            {
                Debug.WriteLine($"Using SVP FFmpeg: {svp.FFmpegPath}");
                return svp.FFmpegPath;
            }
        }

        // 2. Custom path (validate for security)
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (!IsValidPath(customPath))
            {
                Debug.WriteLine($"[FFmpeg] Custom path rejected (invalid characters): {customPath}");
            }
            else if (File.Exists(customPath))
            {
                Debug.WriteLine($"Using custom FFmpeg: {customPath}");
                return customPath;
            }
        }

        // 3. System PATH (test if 'ffmpeg' command works)
        if (IsFFmpegInPath())
        {
            Debug.WriteLine("Using FFmpeg from system PATH");
            return "ffmpeg";
        }

        // 4. Common installation locations
        var commonPaths = new[]
        {
            @"C:\Program Files\Shotcut\ffmpeg.exe",
            @"C:\Program Files (x86)\Shotcut\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Shotcut", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Shotcut", "ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ffmpeg", "bin", "ffmpeg.exe")
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                Debug.WriteLine($"Using FFmpeg (fallback): {path}");
                return path;
            }
        }

        Debug.WriteLine("WARNING: No FFmpeg found in any location");
        return null;
    }

    /// <summary>
    /// Test if FFmpeg is available in system PATH
    /// </summary>
    private bool IsFFmpegInPath()
    {
        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(PROCESS_TIMEOUT_MS);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Populate tool paths within SVP installation
    /// </summary>
    private void PopulateSvpPaths(SvpInstallation installation)
    {
        var basePath = installation.InstallPath;

        // FFmpeg in utils folder
        installation.FFmpegPath = Path.Combine(basePath, "utils", "ffmpeg.exe");

        // Python 3.11 in mpv32 folder
        installation.PythonPath = Path.Combine(basePath, "mpv32", "python.exe");

        // Other utilities
        installation.MediaInfoPath = Path.Combine(basePath, "MediaInfo.dll");
        installation.UtilsFolder = Path.Combine(basePath, "utils");

        // RIFE folder (for AI frame interpolation)
        installation.RifePath = Path.Combine(basePath, "rife");
        installation.HasRife = Directory.Exists(installation.RifePath) &&
                              (File.Exists(Path.Combine(installation.RifePath, "rife.dll")) ||
                               File.Exists(Path.Combine(installation.RifePath, "rife_vs.dll")));

        // Verify FFmpeg NVENC support
        if (File.Exists(installation.FFmpegPath))
        {
            installation.FFmpegHasNvenc = CheckFFmpegNvencSupport(installation.FFmpegPath);
        }
    }

    /// <summary>
    /// Extract version information from components.xml
    /// </summary>
    private void PopulateSvpVersion(SvpInstallation installation)
    {
        try
        {
            var componentsPath = Path.Combine(installation.InstallPath, "components.xml");
            if (!File.Exists(componentsPath))
                return;

            var doc = XDocument.Load(componentsPath);

            // Get core package version
            var corePackage = doc.Descendants("Package")
                .FirstOrDefault(p => p.Element("Name")?.Value == "core");

            if (corePackage != null)
            {
                var appName = doc.Root?.Element("ApplicationName")?.Value ?? "SVP 4 Pro";
                var version = corePackage.Element("Version")?.Value ?? "Unknown";
                installation.Version = $"{appName} {version}";
                installation.ProductName = appName;
            }

            // Get FFmpeg version
            var ffmpegPackage = doc.Descendants("Package")
                .FirstOrDefault(p => p.Element("Name")?.Value == "deps.ffmpeg32");

            if (ffmpegPackage != null)
            {
                installation.FFmpegVersion = ffmpegPackage.Element("Version")?.Value ?? "Unknown";
            }

            // Get Python version
            var pythonPackage = doc.Descendants("Package")
                .FirstOrDefault(p => p.Element("Name")?.Value == "deps.python32");

            if (pythonPackage != null)
            {
                installation.PythonVersion = pythonPackage.Element("Version")?.Value ?? "Unknown";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to parse SVP components.xml: {ex.Message}");
            installation.Version = "SVP 4 Pro (version unknown)";
        }
    }

    /// <summary>
    /// Check if FFmpeg build supports NVENC
    /// </summary>
    private bool CheckFFmpegNvencSupport(string ffmpegPath)
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
            var encoderOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return encoderOutput.Contains("h264_nvenc") || encoderOutput.Contains("hevc_nvenc");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check NVENC support: {ex.Message}");
        }

        return false;
    }
}

/// <summary>
/// Information about detected SVP installation
/// </summary>
public class SvpInstallation
{
    public bool IsInstalled { get; set; }
    public string InstallPath { get; set; } = string.Empty;
    public string Version { get; set; } = "Not detected";
    public string ProductName { get; set; } = "SVP 4 Pro";

    // Tool paths
    public string FFmpegPath { get; set; } = string.Empty;
    public string PythonPath { get; set; } = string.Empty;
    public string MediaInfoPath { get; set; } = string.Empty;
    public string UtilsFolder { get; set; } = string.Empty;
    public string RifePath { get; set; } = string.Empty;

    // Version information
    public string FFmpegVersion { get; set; } = "Unknown";
    public string PythonVersion { get; set; } = "Unknown";

    // Capabilities
    public bool FFmpegHasNvenc { get; set; }
    public bool HasRife { get; set; }

    /// <summary>
    /// Human-readable summary
    /// </summary>
    public string Summary => IsInstalled
        ? $"{Version} - FFmpeg {FFmpegVersion}, Python {PythonVersion}"
        : "Not installed";
}
