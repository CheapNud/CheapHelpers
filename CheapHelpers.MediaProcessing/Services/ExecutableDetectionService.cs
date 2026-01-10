using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CheapHelpers.MediaProcessing.Services;

/// <summary>
/// Generic service for detecting executables in priority order:
/// 1. Custom paths (from settings)
/// 2. SVP installation (preferred for FFmpeg)
/// 3. System PATH
/// 4. Common installation directories
/// </summary>
/// <remarks>
/// PLATFORM REQUIREMENT: This service requires Windows operating system.
/// Uses the Windows 'where' command for PATH detection.
/// </remarks>
[SupportedOSPlatform("windows")]
public class ExecutableDetectionService(SvpDetectionService svpDetection)
{
    private const int PROCESS_TIMEOUT_MS = 1000;

    /// <summary>
    /// Characters that are not allowed in executable names for security
    /// </summary>
    private static readonly char[] InvalidExecutableChars = ['&', '|', ';', '$', '`', '"', '\'', '<', '>', '(', ')', '{', '}', '[', ']', '\n', '\r'];

    /// <summary>
    /// Validates an executable name to prevent command injection
    /// </summary>
    /// <param name="executableName">The name to validate</param>
    /// <returns>True if the name is safe to use</returns>
    private static bool IsValidExecutableName(string? executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
            return false;

        // Check for command injection characters
        if (executableName.IndexOfAny(InvalidExecutableChars) >= 0)
            return false;

        // Check for path traversal
        if (executableName.Contains(".."))
            return false;

        // Check for invalid filename characters (but allow path separators for full paths)
        var fileName = Path.GetFileName(executableName);
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Validates an executable name and throws if invalid
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the executable name contains invalid characters</exception>
    private static void ValidateExecutableName(string executableName, string paramName)
    {
        if (!IsValidExecutableName(executableName))
        {
            throw new ArgumentException(
                $"Invalid executable name '{executableName}'. Name contains characters that could be used for command injection.",
                paramName);
        }
    }
    /// <summary>
    /// Auto-detect common media executables
    /// </summary>
    public DetectedExecutables DetectAll()
    {
        var detected = new DetectedExecutables
        {
            FFmpegPath = DetectFFmpeg(useSvpEncoders: true, customPath: null),
            FFprobePath = DetectFFprobe(useSvpEncoders: true, customPath: null),
            MeltPath = DetectMelt(customPath: null)
        };

        Debug.WriteLine("=== Executable Detection ===");
        Debug.WriteLine($"FFmpeg: {detected.FFmpegPath ?? "NOT FOUND"}");
        Debug.WriteLine($"FFprobe: {detected.FFprobePath ?? "NOT FOUND"}");
        Debug.WriteLine($"Melt: {detected.MeltPath ?? "NOT FOUND"}");
        Debug.WriteLine("============================");

        return detected;
    }

    /// <summary>
    /// Detect FFmpeg with priority order
    /// </summary>
    public string? DetectFFmpeg(bool useSvpEncoders, string? customPath)
    {
        // 1. Custom path
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            Debug.WriteLine($"[FFmpeg] Using custom path: {customPath}");
            return customPath;
        }

        // 2. SVP installation (if enabled)
        if (useSvpEncoders)
        {
            var svp = svpDetection.DetectSvpInstallation();
            if (svp.IsInstalled && File.Exists(svp.FFmpegPath))
            {
                Debug.WriteLine($"[FFmpeg] Using SVP: {svp.FFmpegPath}");
                return svp.FFmpegPath;
            }
        }

        // 3. System PATH
        if (IsExecutableInPath("ffmpeg"))
        {
            var pathLocation = GetExecutablePathFromCommand("ffmpeg");
            Debug.WriteLine($"[FFmpeg] Found in system PATH: {pathLocation ?? "ffmpeg"}");
            return pathLocation ?? "ffmpeg";
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
                Debug.WriteLine($"[FFmpeg] Using: {path}");
                return path;
            }
        }

        Debug.WriteLine("[FFmpeg] NOT FOUND");
        return null;
    }

    /// <summary>
    /// Detect Melt (Shotcut/MLT renderer) with priority order
    /// </summary>
    public string? DetectMelt(string? customPath)
    {
        // 1. Custom path
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            Debug.WriteLine($"[Melt] Using custom path: {customPath}");
            return customPath;
        }

        // 2. System PATH
        if (IsExecutableInPath("melt"))
        {
            var pathLocation = GetExecutablePathFromCommand("melt");
            Debug.WriteLine($"[Melt] Found in system PATH: {pathLocation ?? "melt"}");
            return pathLocation ?? "melt";
        }

        // 3. Common installation locations (Shotcut)
        var commonPaths = new[]
        {
            @"C:\Program Files\Shotcut\melt.exe",
            @"C:\Program Files (x86)\Shotcut\melt.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Shotcut", "melt.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Shotcut", "melt.exe")
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                Debug.WriteLine($"[Melt] Using: {path}");
                return path;
            }
        }

        Debug.WriteLine("[Melt] NOT FOUND");
        return null;
    }

    /// <summary>
    /// Detect FFprobe with priority order
    /// </summary>
    public string? DetectFFprobe(bool useSvpEncoders, string? customPath)
    {
        // 1. Custom path
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            Debug.WriteLine($"[FFprobe] Using custom path: {customPath}");
            return customPath;
        }

        // 2. System PATH
        if (IsExecutableInPath("ffprobe"))
        {
            var pathLocation = GetExecutablePathFromCommand("ffprobe");
            Debug.WriteLine($"[FFprobe] Found in system PATH: {pathLocation ?? "ffprobe"}");
            return pathLocation ?? "ffprobe";
        }

        // 3. Common installation locations
        var commonPaths = new[]
        {
            @"C:\Program Files\Shotcut\ffprobe.exe",
            @"C:\Program Files (x86)\Shotcut\ffprobe.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Shotcut", "ffprobe.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Shotcut", "ffprobe.exe"),
            @"C:\ffmpeg\bin\ffprobe.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ffmpeg", "bin", "ffprobe.exe")
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                Debug.WriteLine($"[FFprobe] Using: {path}");
                return path;
            }
        }

        Debug.WriteLine("[FFprobe] NOT FOUND");
        return null;
    }

    /// <summary>
    /// Generic executable detection with custom search paths
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when executableName contains invalid characters</exception>
    public string? DetectExecutable(string executableName, string? customPath, params string[] searchPaths)
    {
        ValidateExecutableName(executableName, nameof(executableName));

        // 1. Custom path (validate if provided)
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (!IsValidExecutableName(customPath))
            {
                Debug.WriteLine($"[DetectExecutable] Custom path rejected (invalid characters): {customPath}");
            }
            else if (File.Exists(customPath))
            {
                return customPath;
            }
        }

        // 2. System PATH
        if (IsExecutableInPath(executableName))
        {
            return GetExecutablePathFromCommand(executableName) ?? executableName;
        }

        // 3. Search paths (only check valid paths)
        foreach (var path in searchPaths)
        {
            if (IsValidExecutableName(path) && File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if executable exists in system PATH
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when executableName contains invalid characters</exception>
    public bool IsExecutableInPath(string executableName)
    {
        ValidateExecutableName(executableName, nameof(executableName));

        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(PROCESS_TIMEOUT_MS);

            return process.ExitCode == 0 || process.ExitCode == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get full path of executable from PATH using 'where' command (Windows)
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when executableName contains invalid characters</exception>
    public string? GetExecutablePathFromCommand(string executableName)
    {
        ValidateExecutableName(executableName, nameof(executableName));

        try
        {
            // Use ArgumentList to prevent command injection
            var startInfo = new ProcessStartInfo("where")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(executableName);

            using var process = new SysProcess { StartInfo = startInfo };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var firstPath = output.Split('\n')[0].Trim();
                return File.Exists(firstPath) ? firstPath : null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to find {executableName} in PATH: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Get version string from an executable
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when executablePath contains invalid characters</exception>
    public async Task<string?> GetExecutableVersionAsync(string executablePath, string versionArgument = "-version")
    {
        ValidateExecutableName(executablePath, nameof(executablePath));

        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = versionArgument,
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
                return output.Split('\n')[0].Trim();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get version for {executablePath}: {ex.Message}");
        }

        return null;
    }
}

/// <summary>
/// Result of executable detection
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
