using SysProcess = System.Diagnostics.Process;
using System.Diagnostics;

namespace CheapHelpers.MediaProcessing.Services.Utilities;

/// <summary>
/// Manages external process execution with proper output handling
/// </summary>
public class ProcessManager
{
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

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutput?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
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
            result.StandardOutput = outputBuilder.ToString();
            result.StandardError = errorBuilder.ToString();
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
        int timeoutMs = 30000)
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
    /// Start a long-running process with progress reporting
    /// </summary>
    public SysProcess StartLongRunning(
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

        return process;
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
