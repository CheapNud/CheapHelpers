using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;

namespace CheapHelpers.MediaProcessing.Services.Utilities;

/// <summary>
/// Manages external process execution with proper output handling
/// </summary>
public class ProcessManager
{
    private const int DEFAULT_SYNC_TIMEOUT_MS = 30000;

    /// <summary>
    /// Run a process and capture output
    /// </summary>
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int timeoutMs = 0,
        CancellationToken cancellationToken = default,
        Action<string>? onOutput = null,
        Action<string>? onError = null)
    {
        var result = new ProcessResult();

        using var process = new SysProcess
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        var outputLock = new object();
        var errorLock = new object();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (outputLock)
                {
                    outputBuilder.AppendLine(e.Data);
                }
                onOutput?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (errorLock)
                {
                    errorBuilder.AppendLine(e.Data);
                }
                onError?.Invoke(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (timeoutMs > 0)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill(true);
                    result.TimedOut = true;
                }
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            result.ExitCode = process.ExitCode;
            lock (outputLock) { result.StandardOutput = outputBuilder.ToString(); }
            lock (errorLock) { result.StandardError = errorBuilder.ToString(); }
            result.Success = process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            result.Cancelled = true;
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.StandardError = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Run a process synchronously with timeout
    /// </summary>
    public ProcessResult Run(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int timeoutMs = DEFAULT_SYNC_TIMEOUT_MS)
    {
        var result = new ProcessResult();

        using var process = new SysProcess
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (process.WaitForExit(timeoutMs))
            {
                result.StandardOutput = outputTask.GetAwaiter().GetResult();
                result.StandardError = errorTask.GetAwaiter().GetResult();
                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
            }
            else
            {
                process.Kill(true);
                result.TimedOut = true;
            }
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.StandardError = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Gracefully shut down a process with optional force kill
    /// </summary>
    public static async Task GracefulShutdownAsync(
        SysProcess process,
        int gracefulTimeoutMs = 3000,
        string processName = "Process")
    {
        if (process.HasExited)
        {
            Debug.WriteLine($"[{processName}] Already exited");
            return;
        }

        try
        {
            // Try to close the process gracefully
            Debug.WriteLine($"[{processName}] Attempting graceful shutdown...");
            process.CloseMainWindow();

            // Wait for graceful exit
            using var cts = new CancellationTokenSource(gracefulTimeoutMs);
            try
            {
                await process.WaitForExitAsync(cts.Token);
                Debug.WriteLine($"[{processName}] Graceful shutdown successful");
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown failed, force kill
                Debug.WriteLine($"[{processName}] Graceful shutdown timed out, forcing kill...");
                process.Kill(true);
                Debug.WriteLine($"[{processName}] Force killed");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{processName}] Error during shutdown: {ex.Message}");
            try { process.Kill(true); } catch { }
        }
    }

    /// <summary>
    /// Start a long-running process with progress reporting.
    /// Returns a <see cref="ManagedProcess"/> that must be disposed when no longer needed.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: The caller is responsible for disposing the returned <see cref="ManagedProcess"/>.
    /// Use with 'using' statement or call Dispose() explicitly to prevent process leaks.
    /// </remarks>
    /// <example>
    /// <code>
    /// using var managed = processManager.StartLongRunning("ffmpeg", "-i input.mp4 output.mkv");
    /// await managed.WaitForExitAsync();
    /// </code>
    /// </example>
    public ManagedProcess StartLongRunning(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null)
    {
        var process = new SysProcess
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        if (onOutput != null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) onOutput(e.Data);
            };
        }

        if (onError != null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) onError(e.Data);
            };
        }

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return new ManagedProcess(process);
    }
}

/// <summary>
/// Wrapper for long-running processes that ensures proper disposal.
/// Implements IDisposable to properly clean up process resources.
/// </summary>
/// <remarks>
/// IMPORTANT: Always dispose this wrapper using 'using' statement or explicit Dispose() call.
/// This class wraps a System.Diagnostics.Process and provides:
/// - Automatic disposal of process resources
/// - Access to the underlying process for monitoring
/// - Helper methods for graceful shutdown
/// - Leak detection via finalizer (logs warning if not properly disposed)
/// </remarks>
public sealed class ManagedProcess : IDisposable
{
    private readonly SysProcess _process;
    private readonly string _processInfo;
    private readonly DateTime _startTime;
    private bool _disposed;

    internal ManagedProcess(SysProcess process)
    {
        _process = process;
        _startTime = DateTime.UtcNow;
        // Capture info for leak detection logging (process may exit before finalizer runs)
        _processInfo = $"PID={process.Id}, FileName={process.StartInfo.FileName}";
    }

    /// <summary>
    /// Gets the underlying process. Use with caution - prefer using ManagedProcess methods.
    /// </summary>
    public SysProcess Process => _process;

    /// <summary>
    /// Gets a value indicating whether the process has exited
    /// </summary>
    public bool HasExited => _process.HasExited;

    /// <summary>
    /// Gets the exit code of the process (only valid after process has exited)
    /// </summary>
    public int ExitCode => _process.ExitCode;

    /// <summary>
    /// Gets the process ID
    /// </summary>
    public int Id => _process.Id;

    /// <summary>
    /// Wait for the process to exit asynchronously
    /// </summary>
    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _process.WaitForExitAsync(cancellationToken);

    /// <summary>
    /// Wait for the process to exit synchronously with optional timeout
    /// </summary>
    public bool WaitForExit(int milliseconds) => _process.WaitForExit(milliseconds);

    /// <summary>
    /// Wait for the process to exit synchronously
    /// </summary>
    public void WaitForExit() => _process.WaitForExit();

    /// <summary>
    /// Kill the process immediately
    /// </summary>
    /// <param name="entireProcessTree">If true, kills the process and all child processes</param>
    public void Kill(bool entireProcessTree = false) => _process.Kill(entireProcessTree);

    /// <summary>
    /// Gracefully shut down the process with optional force kill after timeout
    /// </summary>
    public async Task GracefulShutdownAsync(int gracefulTimeoutMs = 3000)
    {
        await ProcessManager.GracefulShutdownAsync(_process, gracefulTimeoutMs, $"Process-{_process.Id}");
    }

    /// <summary>
    /// Disposes the process resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(true);
            }
        }
        catch { }

        _process.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer for leak detection. Logs warning if process wasn't properly disposed.
    /// </summary>
    /// <remarks>
    /// This finalizer does NOT attempt to clean up the process (I/O in finalizers is dangerous).
    /// It only logs a warning to help developers identify disposal issues during development.
    /// </remarks>
    ~ManagedProcess()
    {
        if (!_disposed)
        {
            // Log warning about leaked process - this helps identify missing 'using' statements
            var lifetime = DateTime.UtcNow - _startTime;
            Debug.WriteLine($"[LEAK WARNING] ManagedProcess not disposed! {_processInfo}, Lifetime={lifetime.TotalSeconds:F1}s. " +
                           "Use 'using' statement or call Dispose() explicitly.");

            // Don't try to clean up - just mark as disposed to prevent double-warning
            _disposed = true;
        }
    }
}

/// <summary>
/// Result of a process execution
/// </summary>
public class ProcessResult
{
    public int ExitCode { get; set; } = -1;
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool TimedOut { get; set; }
    public bool Cancelled { get; set; }
    public Exception? Exception { get; set; }

    public bool HasError => !Success || !string.IsNullOrWhiteSpace(StandardError) || Exception != null;
}
