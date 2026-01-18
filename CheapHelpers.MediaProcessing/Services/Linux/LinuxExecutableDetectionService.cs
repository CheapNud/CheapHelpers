using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CheapHelpers.MediaProcessing.Services.Linux;

/// <summary>
/// Linux implementation for detecting executables.
/// Uses 'which' command and PATH enumeration.
/// </summary>
[UnsupportedOSPlatform("windows")]
public class LinuxExecutableDetectionService
{
    private const int PROCESS_TIMEOUT_MS = 1000;

    /// <summary>
    /// Characters that are not allowed in executable names for security
    /// </summary>
    private static readonly char[] InvalidExecutableChars = ['&', '|', ';', '$', '`', '"', '\'', '<', '>', '(', ')', '{', '}', '[', ']', '\n', '\r'];

    /// <summary>
    /// Validates an executable name to prevent command injection
    /// </summary>
    private static bool IsValidExecutableName(string? executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
            return false;

        if (executableName.IndexOfAny(InvalidExecutableChars) >= 0)
            return false;

        if (executableName.Contains(".."))
            return false;

        var fileName = Path.GetFileName(executableName);
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        return true;
    }

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
            FFmpegPath = DetectFFmpeg(useSvpEncoders: false, customPath: null),
            FFprobePath = DetectFFprobe(useSvpEncoders: false, customPath: null),
            MeltPath = DetectMelt(customPath: null)
        };

        Debug.WriteLine("=== Linux Executable Detection ===");
        Debug.WriteLine($"FFmpeg: {detected.FFmpegPath ?? "NOT FOUND"}");
        Debug.WriteLine($"FFprobe: {detected.FFprobePath ?? "NOT FOUND"}");
        Debug.WriteLine($"Melt: {detected.MeltPath ?? "NOT FOUND"}");
        Debug.WriteLine("==================================");

        return detected;
    }

    /// <summary>
    /// Detect FFmpeg with priority order (SVP not available on Linux)
    /// </summary>
    public string? DetectFFmpeg(bool useSvpEncoders, string? customPath)
    {
        // 1. Custom path
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            Debug.WriteLine($"[FFmpeg] Using custom path: {customPath}");
            return customPath;
        }

        // 2. System PATH via 'which'
        var whichPath = GetExecutablePathFromWhich("ffmpeg");
        if (!string.IsNullOrEmpty(whichPath))
        {
            Debug.WriteLine($"[FFmpeg] Found via which: {whichPath}");
            return whichPath;
        }

        // 3. Common Linux installation locations
        var commonPaths = new[]
        {
            "/usr/bin/ffmpeg",
            "/usr/local/bin/ffmpeg",
            "/opt/ffmpeg/bin/ffmpeg",
            "/app/bin/ffmpeg",  // Docker
            "/snap/bin/ffmpeg"
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

        // 2. System PATH via 'which'
        var whichPath = GetExecutablePathFromWhich("ffprobe");
        if (!string.IsNullOrEmpty(whichPath))
        {
            Debug.WriteLine($"[FFprobe] Found via which: {whichPath}");
            return whichPath;
        }

        // 3. Common Linux installation locations
        var commonPaths = new[]
        {
            "/usr/bin/ffprobe",
            "/usr/local/bin/ffprobe",
            "/opt/ffmpeg/bin/ffprobe",
            "/app/bin/ffprobe"
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
    /// Detect Melt (MLT renderer)
    /// </summary>
    public string? DetectMelt(string? customPath)
    {
        // 1. Custom path
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            Debug.WriteLine($"[Melt] Using custom path: {customPath}");
            return customPath;
        }

        // 2. System PATH via 'which'
        var whichPath = GetExecutablePathFromWhich("melt");
        if (!string.IsNullOrEmpty(whichPath))
        {
            Debug.WriteLine($"[Melt] Found via which: {whichPath}");
            return whichPath;
        }

        // 3. Common installation locations
        var commonPaths = new[]
        {
            "/usr/bin/melt",
            "/usr/local/bin/melt"
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
    /// Generic executable detection with custom search paths
    /// </summary>
    public string? DetectExecutable(string executableName, string? customPath, params string[] searchPaths)
    {
        ValidateExecutableName(executableName, nameof(executableName));

        // 1. Custom path
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

        // 2. System PATH via 'which'
        var whichPath = GetExecutablePathFromWhich(executableName);
        if (!string.IsNullOrEmpty(whichPath))
        {
            return whichPath;
        }

        // 3. Search paths
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
    /// Get full path of executable using 'which' command
    /// </summary>
    public string? GetExecutablePathFromWhich(string executableName)
    {
        ValidateExecutableName(executableName, nameof(executableName));

        try
        {
            using var process = new SysProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = executableName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(PROCESS_TIMEOUT_MS);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) && File.Exists(output))
            {
                Debug.WriteLine($"[which] Found {executableName} at: {output}");
                return output;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to run 'which {executableName}': {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Get full path of executable from PATH environment variable
    /// </summary>
    public string? GetExecutablePathFromCommand(string executableName)
    {
        ValidateExecutableName(executableName, nameof(executableName));

        // First try 'which'
        var whichPath = GetExecutablePathFromWhich(executableName);
        if (!string.IsNullOrEmpty(whichPath))
        {
            return whichPath;
        }

        // Fallback: enumerate PATH directories
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv))
            {
                return null;
            }

            // Linux PATH separator is colon
            var pathDirs = pathEnv.Split(':', StringSplitOptions.RemoveEmptyEntries);

            foreach (var dir in pathDirs)
            {
                if (!Directory.Exists(dir))
                    continue;

                var fullPath = Path.Combine(dir, executableName);
                if (File.Exists(fullPath))
                {
                    Debug.WriteLine($"[GetExecutablePath] Found {executableName} at: {fullPath}");
                    return fullPath;
                }
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
