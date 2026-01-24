using System.Diagnostics;
using SysProcess = System.Diagnostics.Process;

namespace CheapHelpers.MediaProcessing.Utilities;

/// <summary>
/// Helper methods for safe process execution with timeout and cancellation support.
/// </summary>
internal static class ProcessHelper
{
    /// <summary>
    /// Default process timeout in milliseconds (30 seconds)
    /// </summary>
    public const int DefaultTimeoutMs = 30_000;

    /// <summary>
    /// Run a process asynchronously with proper timeout and cancellation handling.
    /// </summary>
    /// <param name="process">The process to run (must be configured but not started)</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (exitCode, stdout, stderr). Exit code is -1 if timed out or cancelled.</returns>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        SysProcess process,
        int timeoutMs = DefaultTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            process.Start();

            // Read both streams concurrently to prevent deadlock when buffers fill
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            return (process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            // Kill the entire process tree on timeout or cancellation
            KillProcessSafely(process);

            if (cancellationToken.IsCancellationRequested)
                throw; // Re-throw if user cancelled

            // Timeout - return failure
            Debug.WriteLine($"[ProcessHelper] Process timed out after {timeoutMs}ms");
            return (-1, string.Empty, "Process timed out");
        }
    }

    /// <summary>
    /// Run a process synchronously with timeout.
    /// </summary>
    /// <param name="process">The process to run (must be configured but not started)</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <returns>Tuple of (exitCode, stdout, stderr). Exit code is -1 if timed out.</returns>
    public static (int ExitCode, string StdOut, string StdErr) Run(
        SysProcess process,
        int timeoutMs = DefaultTimeoutMs)
    {
        ArgumentNullException.ThrowIfNull(process);

        try
        {
            process.Start();

            // Read output after process exits to avoid deadlock
            if (!process.WaitForExit(timeoutMs))
            {
                KillProcessSafely(process);
                Debug.WriteLine($"[ProcessHelper] Process timed out after {timeoutMs}ms");
                return (-1, string.Empty, "Process timed out");
            }

            // After WaitForExit returns, it's safe to read remaining output
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            return (process.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            KillProcessSafely(process);
            Debug.WriteLine($"[ProcessHelper] Process failed: {ex.Message}");
            return (-1, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Safely kill a process and its entire process tree.
    /// </summary>
    private static void KillProcessSafely(SysProcess process)
    {
        try
        {
            if (!process.HasExited)
            {
                // Kill entire process tree to prevent orphaned child processes
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            // Best effort cleanup - process may have already exited
            Debug.WriteLine($"[ProcessHelper] Kill failed (may have already exited): {ex.Message}");
        }
    }

    /// <summary>
    /// Create a process configured for command execution with output capture.
    /// </summary>
    public static SysProcess CreateProcess(string fileName, string? arguments = null)
    {
        var process = new SysProcess
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!string.IsNullOrEmpty(arguments))
        {
            process.StartInfo.Arguments = arguments;
        }

        return process;
    }
}
