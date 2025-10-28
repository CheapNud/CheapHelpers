using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CheapHelpers.Process;

/// <summary>
/// Generic process execution wrapper with progress tracking, timeout handling, and cancellation support
/// </summary>
public class ProcessExecutor
{
    /// <summary>
    /// Execute a process with optional progress tracking and cancellation
    /// </summary>
    /// <param name="executable">Path to the executable</param>
    /// <param name="arguments">Command-line arguments</param>
    /// <param name="options">Process execution options</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process execution result</returns>
    public static async Task<ProcessResult> ExecuteAsync(
        string executable,
        string arguments,
        ProcessExecutorOptions? options = null,
        IProgress<ProcessProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ProcessExecutorOptions();

        Debug.WriteLine($"Executing: {executable} {arguments}");

        var startTime = DateTime.UtcNow;
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var wasKilled = false;
        var timedOut = false;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = options.WorkingDirectory,
            RedirectStandardOutput = options.CaptureOutput,
            RedirectStandardError = options.CaptureOutput,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Add environment variables if specified
        if (options.EnvironmentVariables != null)
        {
            foreach (var (key, value) in options.EnvironmentVariables)
            {
                processStartInfo.Environment[key] = value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };

        if (options.CaptureOutput)
        {
            // Set up output handlers with progress tracking
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                outputBuilder.AppendLine(e.Data);
                Debug.WriteLine($"[stdout] {e.Data}");

                // Try to extract progress if patterns are configured
                if (options.ProgressPatterns != null && progress != null)
                {
                    var percentage = TryExtractProgress(e.Data, options.ProgressPatterns);
                    if (percentage.HasValue)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        progress.Report(new ProcessProgress(percentage.Value, e.Data, elapsed));
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                errorBuilder.AppendLine(e.Data);
                Debug.WriteLine($"[stderr] {e.Data}");

                // Some processes output progress to stderr (like FFmpeg)
                if (options.ProgressPatterns != null && progress != null)
                {
                    var percentage = TryExtractProgress(e.Data, options.ProgressPatterns);
                    if (percentage.HasValue)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        progress.Report(new ProcessProgress(percentage.Value, e.Data, elapsed));
                    }
                }
            };
        }

        process.Start();

        if (options.CaptureOutput)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        // Wait for completion with timeout and cancellation support
        try
        {
            if (options.Timeout.HasValue)
            {
                var timeoutTask = Task.Delay(options.Timeout.Value, cancellationToken);
                var processTask = process.WaitForExitAsync(cancellationToken);

                var completedTask = await Task.WhenAny(processTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    Debug.WriteLine($"Process timed out after {options.Timeout.Value}");
                    timedOut = true;
                    wasKilled = true;
                    try { process.Kill(); } catch { }
                }
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested
            Debug.WriteLine("Process execution cancelled");
            wasKilled = true;
            try { process.Kill(); } catch { }
        }

        var duration = DateTime.UtcNow - startTime;

        return new ProcessResult(
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString(),
            duration,
            wasKilled,
            timedOut);
    }

    /// <summary>
    /// Execute two processes with piping (source stdout -> destination stdin)
    /// </summary>
    /// <param name="source">Source process information</param>
    /// <param name="destination">Destination process information</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Combined process execution result</returns>
    public static async Task<ProcessResult> ExecuteWithPipingAsync(
        ProcessInfo source,
        ProcessInfo destination,
        IProgress<ProcessProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Executing with piping: {source.Executable} {source.Arguments} | {destination.Executable} {destination.Arguments}");

        var startTime = DateTime.UtcNow;
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var wasKilled = false;

        // Configure source process
        var sourceStartInfo = new ProcessStartInfo
        {
            FileName = source.Executable,
            Arguments = source.Arguments,
            WorkingDirectory = source.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Configure destination process
        var destinationStartInfo = new ProcessStartInfo
        {
            FileName = destination.Executable,
            Arguments = destination.Arguments,
            WorkingDirectory = destination.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var sourceProcess = new System.Diagnostics.Process { StartInfo = sourceStartInfo };
        using var destinationProcess = new System.Diagnostics.Process { StartInfo = destinationStartInfo };

        // Set up error monitoring for source
        sourceProcess.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            errorBuilder.AppendLine($"[source] {e.Data}");
            Debug.WriteLine($"[source stderr] {e.Data}");

            // Try to extract progress from source stderr
            if (progress != null)
            {
                var percentage = TryExtractProgress(e.Data, ProgressPattern.GetCommonPatterns());
                if (percentage.HasValue)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    progress.Report(new ProcessProgress(percentage.Value, e.Data, elapsed));
                }
            }
        };

        // Set up output/error monitoring for destination
        destinationProcess.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            outputBuilder.AppendLine(e.Data);
            Debug.WriteLine($"[destination stdout] {e.Data}");
        };

        destinationProcess.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            errorBuilder.AppendLine($"[destination] {e.Data}");
            Debug.WriteLine($"[destination stderr] {e.Data}");
        };

        try
        {
            // Start both processes
            sourceProcess.Start();
            destinationProcess.Start();

            // Begin async reading
            sourceProcess.BeginErrorReadLine();
            destinationProcess.BeginOutputReadLine();
            destinationProcess.BeginErrorReadLine();

            // Pipe source output to destination input
            var pipeTask = Task.Run(async () =>
            {
                await sourceProcess.StandardOutput.BaseStream.CopyToAsync(
                    destinationProcess.StandardInput.BaseStream,
                    cancellationToken);
                destinationProcess.StandardInput.Close();
            }, cancellationToken);

            // Wait for both processes to complete
            await Task.WhenAll(
                sourceProcess.WaitForExitAsync(cancellationToken),
                destinationProcess.WaitForExitAsync(cancellationToken),
                pipeTask);

            var duration = DateTime.UtcNow - startTime;

            // Return result based on destination process (final output)
            return new ProcessResult(
                destinationProcess.ExitCode,
                outputBuilder.ToString(),
                errorBuilder.ToString(),
                duration,
                wasKilled,
                false);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Piped process execution cancelled");
            wasKilled = true;

            try
            {
                if (!sourceProcess.HasExited) sourceProcess.Kill();
                if (!destinationProcess.HasExited) destinationProcess.Kill();
            }
            catch { }

            var duration = DateTime.UtcNow - startTime;

            return new ProcessResult(
                -1,
                outputBuilder.ToString(),
                errorBuilder.ToString(),
                duration,
                wasKilled,
                false);
        }
    }

    /// <summary>
    /// Try to extract progress percentage from a line using configured patterns
    /// </summary>
    private static double? TryExtractProgress(string line, List<ProgressPattern> patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(line, pattern.RegexPattern);
            if (match.Success)
            {
                try
                {
                    return pattern.ExtractPercentage(match);
                }
                catch
                {
                    // Pattern matched but extraction failed - continue to next pattern
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Options for process execution
/// </summary>
public class ProcessExecutorOptions
{
    /// <summary>
    /// Working directory for the process
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Maximum time to wait for process completion
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Patterns for extracting progress from stdout/stderr
    /// </summary>
    public List<ProgressPattern>? ProgressPatterns { get; set; }

    /// <summary>
    /// Whether to capture stdout and stderr
    /// </summary>
    public bool CaptureOutput { get; set; } = true;

    /// <summary>
    /// Environment variables to add/override
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}

/// <summary>
/// Pattern for extracting progress percentage from process output
/// </summary>
public class ProgressPattern
{
    /// <summary>
    /// Regular expression pattern to match
    /// </summary>
    public required string RegexPattern { get; init; }

    /// <summary>
    /// Function to extract percentage (0-100) from regex match
    /// </summary>
    public required Func<Match, double> ExtractPercentage { get; init; }

    /// <summary>
    /// Pattern for fraction format: "123/456"
    /// </summary>
    public static ProgressPattern FractionPattern => new()
    {
        RegexPattern = @"(\d+)/(\d+)",
        ExtractPercentage = match =>
        {
            if (int.TryParse(match.Groups[1].Value, out var current) &&
                int.TryParse(match.Groups[2].Value, out var total) &&
                total > 0)
            {
                return (double)current / total * 100.0;
            }
            return 0;
        }
    };

    /// <summary>
    /// Pattern for percentage format: "45%"
    /// </summary>
    public static ProgressPattern PercentPattern => new()
    {
        RegexPattern = @"(\d+)%",
        ExtractPercentage = match =>
        {
            if (double.TryParse(match.Groups[1].Value, out var percent))
            {
                return percent;
            }
            return 0;
        }
    };

    /// <summary>
    /// Pattern for FFmpeg frame output: "frame=123"
    /// Requires total frames to be known for percentage calculation
    /// </summary>
    public static ProgressPattern FFmpegFramePattern => new()
    {
        RegexPattern = @"frame=\s*(\d+)",
        ExtractPercentage = match =>
        {
            // Note: This pattern alone cannot determine percentage without knowing total frames
            // Returns frame number as-is - caller should handle conversion
            if (int.TryParse(match.Groups[1].Value, out var frameNumber))
            {
                return frameNumber;
            }
            return 0;
        }
    };

    /// <summary>
    /// Pattern for VapourSynth vspipe output: "Frame: 123/456"
    /// </summary>
    public static ProgressPattern VsPipeFramePattern => new()
    {
        RegexPattern = @"Frame:\s*(\d+)/(\d+)",
        ExtractPercentage = match =>
        {
            if (int.TryParse(match.Groups[1].Value, out var current) &&
                int.TryParse(match.Groups[2].Value, out var total) &&
                total > 0)
            {
                return (double)current / total * 100.0;
            }
            return 0;
        }
    };

    /// <summary>
    /// Get a list of commonly used progress patterns
    /// </summary>
    public static List<ProgressPattern> GetCommonPatterns() =>
    [
        VsPipeFramePattern,
        FractionPattern,
        PercentPattern
    ];
}

/// <summary>
/// Result of process execution
/// </summary>
/// <param name="ExitCode">Process exit code</param>
/// <param name="StandardOutput">Captured stdout</param>
/// <param name="StandardError">Captured stderr</param>
/// <param name="Duration">Total execution time</param>
/// <param name="WasKilled">Whether the process was forcefully terminated</param>
/// <param name="TimedOut">Whether the process exceeded the timeout</param>
public record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    bool WasKilled,
    bool TimedOut)
{
    /// <summary>
    /// Whether the process completed successfully
    /// </summary>
    public bool Success => ExitCode == 0 && !WasKilled && !TimedOut;
}

/// <summary>
/// Progress information from process execution
/// </summary>
/// <param name="Percentage">Progress percentage (0-100)</param>
/// <param name="CurrentLine">The line that contained progress information</param>
/// <param name="Elapsed">Time elapsed since process started</param>
public record ProcessProgress(
    double Percentage,
    string? CurrentLine,
    TimeSpan? Elapsed);

/// <summary>
/// Information about a process to execute
/// </summary>
/// <param name="Executable">Path to the executable</param>
/// <param name="Arguments">Command-line arguments</param>
/// <param name="WorkingDirectory">Working directory (optional)</param>
public record ProcessInfo(
    string Executable,
    string Arguments,
    string? WorkingDirectory = null);
