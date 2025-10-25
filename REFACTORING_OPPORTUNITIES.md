# Refactoring Opportunities - ShotcutRandomizer to ProcessExecutor

## Overview

This document identifies specific code patterns in ShotcutRandomizer that could be refactored to use the new `ProcessExecutor` utility from CheapHelpers.

## Pattern 1: Basic Process Execution with Progress

### Location
`RifeInterpolationService.cs` - Lines 394-462

### Current Implementation
```csharp
var processInfo = new ProcessStartInfo
{
    FileName = _pythonPath,
    Arguments = arguments,
    WorkingDirectory = _rifeFolderPath,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var process = new Process { StartInfo = processInfo };

var progressPattern = new Regex(@"(\d+)/(\d+)");
var percentPattern = new Regex(@"(\d+)%");

process.OutputDataReceived += (sender, e) =>
{
    if (string.IsNullOrEmpty(e.Data))
        return;

    Debug.WriteLine($"[RIFE] {e.Data}");

    var percentMatch = percentPattern.Match(e.Data);
    if (percentMatch.Success && double.TryParse(percentMatch.Groups[1].Value, out var percent))
    {
        progress?.Report(percent);
    }
    else
    {
        var progressMatch = progressPattern.Match(e.Data);
        if (progressMatch.Success &&
            int.TryParse(progressMatch.Groups[1].Value, out var current) &&
            int.TryParse(progressMatch.Groups[2].Value, out var total) &&
            total > 0)
        {
            progress?.Report((double)current / total * 100);
        }
    }
};

process.ErrorDataReceived += (sender, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
    {
        Debug.WriteLine($"[RIFE ERROR] {e.Data}");
    }
};

process.Start();
process.BeginOutputReadLine();
process.BeginErrorReadLine();

while (!process.HasExited)
{
    if (cancellationToken.IsCancellationRequested)
    {
        try { process.Kill(); } catch { }
        return false;
    }
    await Task.Delay(100, cancellationToken);
}

var success = process.ExitCode == 0;
```

### Refactored with ProcessExecutor
```csharp
var result = await ProcessExecutor.ExecuteAsync(
    executable: _pythonPath,
    arguments: arguments,
    options: new ProcessExecutorOptions
    {
        WorkingDirectory = _rifeFolderPath,
        ProgressPatterns = [
            ProgressPattern.PercentPattern,
            ProgressPattern.FractionPattern
        ]
    },
    progress: progress != null
        ? new Progress<ProcessProgress>(p => progress.Report(p.Percentage))
        : null,
    cancellationToken: cancellationToken);

var success = result.Success;
```

### Benefits
- 67 lines reduced to ~15 lines (78% reduction)
- Consistent error handling
- Built-in cancellation support
- No manual polling loop needed
- Reusable progress patterns

### Effort
**Low** - Direct replacement, minimal testing needed

---

## Pattern 2: Process with Timeout

### Location
`RifeInterpolationService.cs` - Lines 559-594

### Current Implementation
```csharp
var ffmpegProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = ffmpegPath,
        Arguments = ffmpegArgs,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }
};

var errorOutput = new System.Text.StringBuilder();
ffmpegProcess.ErrorDataReceived += (sender, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
    {
        errorOutput.AppendLine(e.Data);
        Debug.WriteLine($"[FFmpeg] {e.Data}");
    }
};

ffmpegProcess.Start();
ffmpegProcess.BeginErrorReadLine();

var completed = ffmpegProcess.WaitForExit(300000); // 5 minutes

if (!completed)
{
    Debug.WriteLine("FFmpeg process timed out after 5 minutes");
    try { ffmpegProcess.Kill(); } catch { }
    return false;
}

if (ffmpegProcess.ExitCode != 0 || !File.Exists(tempVideoIn))
{
    Debug.WriteLine($"FFmpeg exit code: {ffmpegProcess.ExitCode}");
    Debug.WriteLine($"FFmpeg error output: {errorOutput}");
    return false;
}
```

### Refactored with ProcessExecutor
```csharp
var result = await ProcessExecutor.ExecuteAsync(
    executable: ffmpegPath,
    arguments: ffmpegArgs,
    options: new ProcessExecutorOptions
    {
        Timeout = TimeSpan.FromMinutes(5)
    });

if (result.TimedOut)
{
    Debug.WriteLine("FFmpeg process timed out after 5 minutes");
    return false;
}

if (!result.Success || !File.Exists(tempVideoIn))
{
    Debug.WriteLine($"FFmpeg exit code: {result.ExitCode}");
    Debug.WriteLine($"FFmpeg error output: {result.StandardError}");
    return false;
}
```

### Benefits
- Cleaner timeout handling
- Automatic process cleanup on timeout
- Captured output in result object
- No manual StringBuilder needed

### Effort
**Low** - Straightforward replacement

---

## Pattern 3: Process Piping (vspipe -> ffmpeg)

### Location
`RifeInterpolationService.cs` - Lines 274-327

### Current Implementation
```csharp
var vspipeProcess = new ProcessStartInfo
{
    FileName = vspipePath,
    Arguments = $"\"{tempScriptPath}\" - -c y4m",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

var ffmpegProcess = new ProcessStartInfo
{
    FileName = ffmpegExe,
    Arguments = $"-i - -c:v libx264 -preset fast -crf 18 -y \"{outputVideoPath}\"",
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var vspipe = Process.Start(vspipeProcess);
using var ffmpeg = Process.Start(ffmpegProcess);

if (vspipe == null || ffmpeg == null)
{
    throw new InvalidOperationException("Failed to start vspipe or ffmpeg process");
}

var pipeTask = Task.Run(async () =>
{
    await vspipe.StandardOutput.BaseStream.CopyToAsync(ffmpeg.StandardInput.BaseStream, cancellationToken);
    ffmpeg.StandardInput.Close();
});

var progressTask = Task.Run(async () =>
{
    string? line;
    var framePattern = new Regex(@"Frame:\s*(\d+)/(\d+)");

    while ((line = await vspipe.StandardError.ReadLineAsync()) != null)
    {
        Debug.WriteLine($"[vspipe] {line}");

        var match = framePattern.Match(line);
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out var current) &&
            int.TryParse(match.Groups[2].Value, out var total) &&
            total > 0)
        {
            progress?.Report((double)current / total * 100);
        }
    }
});

var ffmpegMonitorTask = Task.Run(async () =>
{
    string? line;
    while ((line = await ffmpeg.StandardError.ReadLineAsync()) != null)
    {
        Debug.WriteLine($"[ffmpeg] {line}");
    }
});

await Task.WhenAll(
    vspipe.WaitForExitAsync(cancellationToken),
    ffmpeg.WaitForExitAsync(cancellationToken),
    pipeTask,
    progressTask,
    ffmpegMonitorTask
);

var success = vspipe.ExitCode == 0 && ffmpeg.ExitCode == 0;
```

### Refactored with ProcessExecutor
```csharp
var sourceProcess = new ProcessInfo(
    Executable: vspipePath,
    Arguments: $"\"{tempScriptPath}\" - -c y4m");

var destinationProcess = new ProcessInfo(
    Executable: ffmpegExe,
    Arguments: $"-i - -c:v libx264 -preset fast -crf 18 -y \"{outputVideoPath}\"");

var result = await ProcessExecutor.ExecuteWithPipingAsync(
    source: sourceProcess,
    destination: destinationProcess,
    progress: progress != null
        ? new Progress<ProcessProgress>(p => progress.Report(p.Percentage))
        : null,
    cancellationToken: cancellationToken);

var success = result.Success;
```

### Benefits
- 70+ lines reduced to ~15 lines (80% reduction)
- Built-in VsPipe progress pattern
- Automatic task coordination
- Proper error handling for both processes
- No manual Task.WhenAll orchestration

### Effort
**Medium** - Requires testing to ensure piping works correctly in all scenarios

---

## Pattern 4: Python Process Availability Check

### Location
`RifeInterpolationService.cs` - Lines 51-76

### Current Implementation
```csharp
private bool IsPythonAvailable(string pythonCommand)
{
    try
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pythonCommand,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit(2000);
        return process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}
```

### Refactored with ProcessExecutor
```csharp
private async Task<bool> IsPythonAvailableAsync(string pythonCommand)
{
    try
    {
        var result = await ProcessExecutor.ExecuteAsync(
            executable: pythonCommand,
            arguments: "--version",
            options: new ProcessExecutorOptions
            {
                Timeout = TimeSpan.FromSeconds(2)
            });

        return result.Success;
    }
    catch
    {
        return false;
    }
}
```

### Benefits
- Consistent timeout handling
- Async/await pattern
- Automatic cleanup

### Effort
**Low** - Minor change, would need to update caller to async

---

## Summary of Opportunities

| Pattern | Location | Lines Saved | Effort | Priority | Estimated Time |
|---------|----------|-------------|--------|----------|----------------|
| Basic Execution with Progress | RifeInterpolationService:394-462 | ~52 | Low | High | 30 min |
| Process with Timeout | RifeInterpolationService:559-594 | ~20 | Low | High | 20 min |
| Process Piping | RifeInterpolationService:274-327 | ~55 | Medium | High | 1 hour |
| Python Check | RifeInterpolationService:51-76 | ~10 | Low | Low | 15 min |

**Total Potential Code Reduction:** ~137 lines
**Total Estimated Effort:** ~2 hours

## Recommended Refactoring Order

1. **Phase 1: Low-hanging fruit (45 minutes)**
   - Basic Execution with Progress
   - Process with Timeout
   - Python Check

2. **Phase 2: Complex patterns (1 hour)**
   - Process Piping (requires more testing)

3. **Phase 3: Verification (30 minutes)**
   - Integration testing
   - Verify all cancellation scenarios
   - Verify progress reporting

## Testing Strategy

After each refactoring:

1. **Unit Tests:** Verify the refactored code works in isolation
2. **Integration Tests:** Ensure end-to-end video processing still works
3. **Cancellation Tests:** Verify cancellation cleanup works correctly
4. **Timeout Tests:** Ensure timeout scenarios are handled properly
5. **Progress Tests:** Confirm progress reporting is accurate

## Additional Notes

- All refactorings maintain the same external API (no breaking changes)
- Debug.WriteLine statements are preserved in ProcessExecutor
- Error messages and logging remain consistent
- CancellationToken handling is improved (no polling loop)
- All refactorings are backwards compatible with existing code
