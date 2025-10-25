# CheapHelpers - Usage Examples

## TemporaryFileManager

### Basic Usage

```csharp
using CheapHelpers.IO;

// Create a temporary file manager
using var tempManager = new TemporaryFileManager();

// Base directory is automatically created
Debug.WriteLine($"Temp directory: {tempManager.BaseDirectory}");

// Create a temporary subdirectory
var framesDir = tempManager.CreateTempDirectory("frames");

// Create a temporary file path
var outputFile = tempManager.CreateTempFile("output.mp4");

// Use the paths...
await ProcessVideoAsync(framesDir, outputFile);

// Cleanup happens automatically when disposed
// Or call manually: tempManager.Cleanup();
```

### Custom Base Directory

```csharp
// Use a specific base directory instead of system temp
using var tempManager = new TemporaryFileManager(@"C:\MyApp\Temp");

var workDir = tempManager.CreateTempDirectory("processing");
```

### Get Directory Size

```csharp
using var tempManager = new TemporaryFileManager();
var workDir = tempManager.CreateTempDirectory("work");

// ... do work ...

// Check directory size
var sizeBytes = tempManager.GetDirectorySize(workDir);
var sizeFormatted = TemporaryFileManager.FormatSize(sizeBytes);

Debug.WriteLine($"Working directory size: {sizeFormatted}");
// Output: "Working directory size: 2.45 GB"
```

### Error Handling

```csharp
using var tempManager = new TemporaryFileManager();

try
{
    var workDir = tempManager.CreateTempDirectory("work");

    // Process that might throw
    await RiskyOperation(workDir);
}
catch (Exception ex)
{
    Debug.WriteLine($"Operation failed: {ex.Message}");
    // Cleanup still happens automatically via Dispose
}
```

## ProcessExecutor

### Basic Process Execution

```csharp
using CheapHelpers.Process;

var result = await ProcessExecutor.ExecuteAsync(
    executable: "ffmpeg",
    arguments: "-i input.mp4 -c:v libx264 output.mp4",
    options: new ProcessExecutorOptions
    {
        WorkingDirectory = @"C:\Videos",
        Timeout = TimeSpan.FromMinutes(10),
        CaptureOutput = true
    });

if (result.Success)
{
    Debug.WriteLine($"Process completed in {result.Duration}");
}
else if (result.TimedOut)
{
    Debug.WriteLine("Process timed out");
}
else
{
    Debug.WriteLine($"Process failed: {result.StandardError}");
}
```

### Progress Tracking with Predefined Patterns

```csharp
var progress = new Progress<ProcessProgress>(p =>
{
    Debug.WriteLine($"Progress: {p.Percentage:F2}% - {p.CurrentLine}");
});

var result = await ProcessExecutor.ExecuteAsync(
    executable: "vspipe",
    arguments: "script.vpy - -c y4m",
    options: new ProcessExecutorOptions
    {
        ProgressPatterns = ProgressPattern.GetCommonPatterns()
    },
    progress: progress);
```

### Custom Progress Pattern

```csharp
var customPattern = new ProgressPattern
{
    RegexPattern = @"Processing frame (\d+) of (\d+)",
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

var result = await ProcessExecutor.ExecuteAsync(
    executable: "my-tool",
    arguments: "process-video input.mp4",
    options: new ProcessExecutorOptions
    {
        ProgressPatterns = [customPattern]
    },
    progress: new Progress<ProcessProgress>(p =>
    {
        Debug.WriteLine($"{p.Percentage:F1}% complete");
    }));
```

### Cancellation Support

```csharp
var cts = new CancellationTokenSource();

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

var result = await ProcessExecutor.ExecuteAsync(
    executable: "long-running-process",
    arguments: "input.dat",
    cancellationToken: cts.Token);

if (result.WasKilled)
{
    Debug.WriteLine("Process was cancelled");
}
```

### Process Piping (vspipe | ffmpeg)

```csharp
var sourceProcess = new ProcessInfo(
    Executable: "vspipe",
    Arguments: "script.vpy - -c y4m",
    WorkingDirectory: @"C:\Videos");

var destinationProcess = new ProcessInfo(
    Executable: "ffmpeg",
    Arguments: "-i - -c:v libx264 -preset fast -crf 18 output.mp4",
    WorkingDirectory: @"C:\Videos");

var progress = new Progress<ProcessProgress>(p =>
{
    Debug.WriteLine($"Encoding: {p.Percentage:F2}%");
});

var result = await ProcessExecutor.ExecuteWithPipingAsync(
    source: sourceProcess,
    destination: destinationProcess,
    progress: progress);

if (result.Success)
{
    Debug.WriteLine($"Piping completed in {result.Duration}");
}
```

### Environment Variables

```csharp
var result = await ProcessExecutor.ExecuteAsync(
    executable: "python",
    arguments: "inference_video.py --input video.mp4",
    options: new ProcessExecutorOptions
    {
        WorkingDirectory = @"C:\RIFE",
        EnvironmentVariables = new Dictionary<string, string>
        {
            ["CUDA_VISIBLE_DEVICES"] = "0",
            ["PYTHONPATH"] = @"C:\RIFE\lib"
        }
    });
```

### Timeout Handling

```csharp
var result = await ProcessExecutor.ExecuteAsync(
    executable: "slow-process",
    arguments: "large-file.dat",
    options: new ProcessExecutorOptions
    {
        Timeout = TimeSpan.FromMinutes(5)
    });

if (result.TimedOut)
{
    Debug.WriteLine("Process exceeded 5 minute timeout");
}
```

## Real-World Example: RIFE Video Processing

```csharp
using CheapHelpers.IO;
using CheapHelpers.Process;

public async Task<bool> ProcessVideoWithRife(
    string inputVideo,
    string outputVideo,
    IProgress<double> progress,
    CancellationToken ct)
{
    using var tempManager = new TemporaryFileManager();

    var vspipeProcess = new ProcessInfo(
        Executable: @"C:\Program Files\VapourSynth\core\vspipe.exe",
        Arguments: $"\"{tempManager.CreateTempFile("script.vpy")}\" - -c y4m");

    var ffmpegProcess = new ProcessInfo(
        Executable: "ffmpeg",
        Arguments: $"-i - -c:v libx264 -preset fast -crf 18 -y \"{outputVideo}\"");

    var processProgress = new Progress<ProcessProgress>(p =>
    {
        progress?.Report(p.Percentage);
    });

    var result = await ProcessExecutor.ExecuteWithPipingAsync(
        source: vspipeProcess,
        destination: ffmpegProcess,
        progress: processProgress,
        cancellationToken: ct);

    if (result.Success)
    {
        var outputSize = new FileInfo(outputVideo).Length;
        Debug.WriteLine($"Created {TemporaryFileManager.FormatSize(outputSize)} file in {result.Duration}");
    }

    return result.Success;
}
```

## Real-World Example: Frame Extraction and Processing

```csharp
using CheapHelpers.IO;
using CheapHelpers.Process;

public async Task<bool> ExtractAndProcessFrames(
    string videoFile,
    IProgress<double> progress,
    CancellationToken ct)
{
    using var tempManager = new TemporaryFileManager();

    // Create directories for frames
    var inputFramesDir = tempManager.CreateTempDirectory("input_frames");
    var outputFramesDir = tempManager.CreateTempDirectory("output_frames");

    // Step 1: Extract frames from video
    var extractResult = await ProcessExecutor.ExecuteAsync(
        executable: "ffmpeg",
        arguments: $"-i \"{videoFile}\" \"{Path.Combine(inputFramesDir, "frame_%06d.png")}\"",
        options: new ProcessExecutorOptions
        {
            Timeout = TimeSpan.FromMinutes(5),
            ProgressPatterns = [ProgressPattern.FFmpegFramePattern]
        },
        progress: new Progress<ProcessProgress>(p => progress?.Report(p.Percentage * 0.33)),
        cancellationToken: ct);

    if (!extractResult.Success)
    {
        Debug.WriteLine($"Frame extraction failed: {extractResult.StandardError}");
        return false;
    }

    var frameCount = Directory.GetFiles(inputFramesDir, "*.png").Length;
    Debug.WriteLine($"Extracted {frameCount} frames");

    // Step 2: Process frames (example: interpolation, upscaling, etc.)
    progress?.Report(33);

    // ... process frames here ...

    progress?.Report(66);

    // Step 3: Encode back to video
    var encodeResult = await ProcessExecutor.ExecuteAsync(
        executable: "ffmpeg",
        arguments: $"-framerate 60 -i \"{Path.Combine(outputFramesDir, "frame_%06d.png")}\" -c:v libx264 -crf 18 output.mp4",
        options: new ProcessExecutorOptions
        {
            Timeout = TimeSpan.FromMinutes(10)
        },
        progress: new Progress<ProcessProgress>(p => progress?.Report(66 + p.Percentage * 0.34)),
        cancellationToken: ct);

    if (encodeResult.Success)
    {
        var dirSize = tempManager.GetDirectorySize(tempManager.BaseDirectory);
        Debug.WriteLine($"Temporary files used: {TemporaryFileManager.FormatSize(dirSize)}");
    }

    return encodeResult.Success;
}
```

## Best Practices

### TemporaryFileManager

1. **Always use `using` statement** to ensure cleanup
2. **Create subdirectories** for organization instead of flat structure
3. **Check directory size** for large operations to prevent disk space issues
4. **Use FormatSize** for user-friendly size display

### ProcessExecutor

1. **Always specify timeout** for long-running processes
2. **Use predefined patterns** when available instead of custom regex
3. **Check result.Success** before assuming process completed
4. **Handle cancellation** gracefully with proper cleanup
5. **Capture output** for debugging (enabled by default)
6. **Use piping** for process chains instead of temporary files

### Combined Usage

1. **Prefer ProcessExecutor over manual Process** for consistency
2. **Use TemporaryFileManager** for all temporary file operations
3. **Combine both** for complex video processing pipelines
4. **Report progress** to keep users informed during long operations
5. **Use Debug.WriteLine** for diagnostics (project standard)
