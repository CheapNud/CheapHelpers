# ProcessExecutor

Generic process execution wrapper with progress tracking, timeout handling, and cancellation support.

## Overview

The `ProcessExecutor` class provides a robust wrapper for executing external processes with features including:
- Progress tracking from stdout/stderr using regex patterns
- Timeout handling
- Cancellation support
- Output/error capture
- Process piping (stdout to stdin)
- Environment variable configuration

## Namespace

```csharp
using CheapHelpers.Process;
```

## Main Method

### ExecuteAsync

Executes a process with optional progress tracking and cancellation.

**Signature:**
```csharp
public static async Task<ProcessResult> ExecuteAsync(
    string executable,
    string arguments,
    ProcessExecutorOptions? options = null,
    IProgress<ProcessProgress>? progress = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `executable`: Path to the executable
- `arguments`: Command-line arguments
- `options`: Process execution options (optional)
- `progress`: Progress reporter for tracking execution progress (optional)
- `cancellationToken`: Cancellation token (optional)

**Returns:** `ProcessResult` containing exit code, output, errors, and execution metadata

**Example:**
```csharp
// Simple execution
var result = await ProcessExecutor.ExecuteAsync(
    "ffmpeg",
    "-i input.mp4 -c:v libx264 output.mp4");

if (result.Success)
{
    Console.WriteLine("Conversion completed!");
}
else
{
    Console.WriteLine($"Failed with exit code: {result.ExitCode}");
    Console.WriteLine($"Error: {result.StandardError}");
}
```

### ExecuteWithPipingAsync

Executes two processes with piping (source stdout → destination stdin).

**Signature:**
```csharp
public static async Task<ProcessResult> ExecuteWithPipingAsync(
    ProcessInfo source,
    ProcessInfo destination,
    IProgress<ProcessProgress>? progress = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `source`: Source process information
- `destination`: Destination process information
- `progress`: Progress reporter (optional)
- `cancellationToken`: Cancellation token (optional)

**Returns:** `ProcessResult` from the destination process

**Example:**
```csharp
// Pipe vspipe output to ffmpeg
var source = new ProcessInfo(
    "vspipe",
    "--y4m script.vpy -",
    workingDirectory: @"C:\Scripts");

var destination = new ProcessInfo(
    "ffmpeg",
    "-i - -c:v libx264 output.mp4",
    workingDirectory: @"C:\Output");

var result = await ProcessExecutor.ExecuteWithPipingAsync(
    source,
    destination,
    progress: new Progress<ProcessProgress>(p =>
    {
        Console.WriteLine($"Progress: {p.Percentage:F1}%");
    }));
```

## Supporting Classes

### ProcessExecutorOptions

Configuration options for process execution.

**Properties:**
- `WorkingDirectory` (string?): Working directory for the process
- `Timeout` (TimeSpan?): Maximum time to wait for process completion
- `ProgressPatterns` (List&lt;ProgressPattern&gt;?): Patterns for extracting progress from output
- `CaptureOutput` (bool): Whether to capture stdout and stderr (default: true)
- `EnvironmentVariables` (Dictionary&lt;string, string&gt;?): Environment variables to add/override

**Example:**
```csharp
var options = new ProcessExecutorOptions
{
    WorkingDirectory = @"C:\Projects",
    Timeout = TimeSpan.FromMinutes(30),
    CaptureOutput = true,
    ProgressPatterns = ProgressPattern.GetCommonPatterns(),
    EnvironmentVariables = new Dictionary<string, string>
    {
        ["PATH"] = @"C:\Tools;%PATH%",
        ["TEMP"] = @"C:\CustomTemp"
    }
};

var result = await ProcessExecutor.ExecuteAsync(
    "build.exe",
    "--release",
    options);
```

### ProcessResult

Result of process execution.

**Properties:**
- `ExitCode` (int): Process exit code
- `StandardOutput` (string): Captured stdout
- `StandardError` (string): Captured stderr
- `Duration` (TimeSpan): Total execution time
- `WasKilled` (bool): Whether the process was forcefully terminated
- `TimedOut` (bool): Whether the process exceeded the timeout
- `Success` (bool): Whether the process completed successfully (ExitCode == 0 && !WasKilled && !TimedOut)

**Example:**
```csharp
var result = await ProcessExecutor.ExecuteAsync("tool.exe", "process");

Console.WriteLine($"Exit Code: {result.ExitCode}");
Console.WriteLine($"Duration: {result.Duration}");
Console.WriteLine($"Success: {result.Success}");

if (!result.Success)
{
    if (result.TimedOut)
        Console.WriteLine("Process timed out");
    else if (result.WasKilled)
        Console.WriteLine("Process was cancelled");
    else
        Console.WriteLine($"Process failed: {result.StandardError}");
}
```

### ProcessProgress

Progress information from process execution.

**Properties:**
- `Percentage` (double): Progress percentage (0-100)
- `CurrentLine` (string?): The line that contained progress information
- `Elapsed` (TimeSpan?): Time elapsed since process started

**Example:**
```csharp
var progress = new Progress<ProcessProgress>(p =>
{
    Console.Clear();
    Console.WriteLine($"Progress: {p.Percentage:F1}%");
    Console.WriteLine($"Elapsed: {p.Elapsed?.ToString(@"hh\:mm\:ss")}");
    Console.WriteLine($"Current: {p.CurrentLine}");
});
```

### ProcessInfo

Information about a process to execute.

**Constructor:**
```csharp
public record ProcessInfo(
    string Executable,
    string Arguments,
    string? WorkingDirectory = null)
```

**Example:**
```csharp
var processInfo = new ProcessInfo(
    "git",
    "clone https://github.com/user/repo.git",
    @"C:\Repositories");
```

## Progress Patterns

### ProgressPattern

Pattern for extracting progress percentage from process output.

**Properties:**
- `RegexPattern` (string): Regular expression pattern to match
- `ExtractPercentage` (Func&lt;Match, double&gt;): Function to extract percentage (0-100) from regex match

### Built-in Patterns

#### FractionPattern
Matches fraction format: "123/456"

```csharp
var pattern = ProgressPattern.FractionPattern;
// Matches: "123/456" → 26.97%
```

#### PercentPattern
Matches percentage format: "45%"

```csharp
var pattern = ProgressPattern.PercentPattern;
// Matches: "45%" → 45.0%
```

#### FFmpegFramePattern
Matches FFmpeg frame output: "frame=123"

```csharp
var pattern = ProgressPattern.FFmpegFramePattern;
// Matches: "frame=123" → 123 (returns frame number, not percentage)
```

#### VsPipeFramePattern
Matches VapourSynth vspipe output: "Frame: 123/456"

```csharp
var pattern = ProgressPattern.VsPipeFramePattern;
// Matches: "Frame: 123/456" → 26.97%
```

#### GetCommonPatterns
Returns a list of commonly used patterns:

```csharp
var patterns = ProgressPattern.GetCommonPatterns();
// Returns: [VsPipeFramePattern, FractionPattern, PercentPattern]
```

### Custom Progress Patterns

```csharp
var customPattern = new ProgressPattern
{
    RegexPattern = @"Processing:\s*(\d+)\s*of\s*(\d+)",
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

var options = new ProcessExecutorOptions
{
    ProgressPatterns = new List<ProgressPattern> { customPattern }
};
```

## Common Use Cases

### Video Encoding with Progress

```csharp
public class VideoEncoder
{
    public async Task<bool> EncodeVideo(
        string inputFile,
        string outputFile,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        var processProgress = new Progress<ProcessProgress>(p =>
        {
            progress.Report(p.Percentage);
        });

        var options = new ProcessExecutorOptions
        {
            Timeout = TimeSpan.FromHours(2),
            ProgressPatterns = new List<ProgressPattern>
            {
                ProgressPattern.FFmpegFramePattern,
                ProgressPattern.PercentPattern
            }
        };

        var result = await ProcessExecutor.ExecuteAsync(
            "ffmpeg",
            $"-i \"{inputFile}\" -c:v libx264 -crf 23 \"{outputFile}\"",
            options,
            processProgress,
            cancellationToken);

        return result.Success;
    }
}
```

### Build System with Timeout

```csharp
public class BuildService
{
    public async Task<ProcessResult> BuildProject(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var options = new ProcessExecutorOptions
        {
            WorkingDirectory = projectPath,
            Timeout = TimeSpan.FromMinutes(15),
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["MSBuildSDKsPath"] = @"C:\Program Files\dotnet\sdk\9.0.0\Sdks"
            }
        };

        var result = await ProcessExecutor.ExecuteAsync(
            "dotnet",
            "build --configuration Release",
            options,
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            if (result.TimedOut)
                throw new TimeoutException("Build timed out after 15 minutes");

            throw new BuildException(
                $"Build failed with exit code {result.ExitCode}",
                result.StandardError);
        }

        return result;
    }
}
```

### VapourSynth to FFmpeg Piping

```csharp
public class VideoProcessor
{
    public async Task ProcessWithVapourSynth(
        string scriptPath,
        string outputPath,
        IProgress<ProcessProgress> progress,
        CancellationToken cancellationToken)
    {
        var vspipe = new ProcessInfo(
            @"C:\VapourSynth\vspipe.exe",
            $"--y4m \"{scriptPath}\" -");

        var ffmpeg = new ProcessInfo(
            "ffmpeg",
            $"-i - -c:v libx264 -preset slow -crf 18 \"{outputPath}\"");

        var result = await ProcessExecutor.ExecuteWithPipingAsync(
            vspipe,
            ffmpeg,
            progress,
            cancellationToken);

        if (!result.Success)
        {
            throw new Exception($"Processing failed: {result.StandardError}");
        }
    }
}
```

### Git Operations with Output Capture

```csharp
public class GitService
{
    public async Task<List<string>> GetBranches(string repositoryPath)
    {
        var options = new ProcessExecutorOptions
        {
            WorkingDirectory = repositoryPath,
            Timeout = TimeSpan.FromSeconds(30)
        };

        var result = await ProcessExecutor.ExecuteAsync(
            "git",
            "branch --list",
            options);

        if (!result.Success)
            throw new Exception($"Git command failed: {result.StandardError}");

        return result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim().TrimStart('*').Trim())
            .ToList();
    }

    public async Task<bool> Clone(
        string repoUrl,
        string targetPath,
        IProgress<ProcessProgress> progress,
        CancellationToken cancellationToken)
    {
        var options = new ProcessExecutorOptions
        {
            Timeout = TimeSpan.FromMinutes(30),
            ProgressPatterns = new List<ProgressPattern>
            {
                ProgressPattern.PercentPattern
            }
        };

        var result = await ProcessExecutor.ExecuteAsync(
            "git",
            $"clone \"{repoUrl}\" \"{targetPath}\"",
            options,
            progress,
            cancellationToken);

        return result.Success;
    }
}
```

### Batch Processing with Progress

```csharp
public class BatchProcessor
{
    public async Task ProcessFiles(
        List<string> files,
        IProgress<double> overallProgress,
        CancellationToken cancellationToken)
    {
        int totalFiles = files.Count;
        int completedFiles = 0;

        foreach (var file in files)
        {
            var fileProgress = new Progress<ProcessProgress>(p =>
            {
                double fileWeight = 100.0 / totalFiles;
                double currentFileProgress = (p.Percentage / 100.0) * fileWeight;
                double overall = (completedFiles * fileWeight) + currentFileProgress;
                overallProgress.Report(overall);
            });

            var result = await ProcessExecutor.ExecuteAsync(
                "processor.exe",
                $"\"{file}\"",
                new ProcessExecutorOptions { Timeout = TimeSpan.FromMinutes(10) },
                fileProgress,
                cancellationToken);

            if (!result.Success)
                throw new Exception($"Failed to process {file}");

            completedFiles++;
        }
    }
}
```

### Custom Tool Execution

```csharp
public class ToolRunner
{
    public async Task<string> RunCustomTool(
        string toolPath,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        // Build arguments from dictionary
        var args = string.Join(" ",
            parameters.Select(kvp => $"--{kvp.Key}=\"{kvp.Value}\""));

        var options = new ProcessExecutorOptions
        {
            Timeout = TimeSpan.FromMinutes(5),
            WorkingDirectory = Path.GetDirectoryName(toolPath),
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["CUSTOM_CONFIG"] = @"C:\Config\tool.ini"
            }
        };

        var result = await ProcessExecutor.ExecuteAsync(
            toolPath,
            args,
            options,
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Tool execution failed ({result.Duration}):\n{result.StandardError}");
        }

        return result.StandardOutput;
    }
}
```

## Tips and Best Practices

1. **Always Handle Timeouts**: Set reasonable timeouts to prevent hanging processes:
   ```csharp
   var options = new ProcessExecutorOptions
   {
       Timeout = TimeSpan.FromMinutes(30)
   };
   ```

2. **Use Cancellation Tokens**: Allow users to cancel long-running operations:
   ```csharp
   var cts = new CancellationTokenSource();
   var result = await ProcessExecutor.ExecuteAsync(
       executable, args, cancellationToken: cts.Token);
   ```

3. **Progress Reporting**: Test your progress patterns with actual tool output to ensure accurate tracking.

4. **Error Handling**: Always check `result.Success` and handle different failure modes:
   ```csharp
   if (result.TimedOut)
       HandleTimeout();
   else if (result.WasKilled)
       HandleCancellation();
   else if (result.ExitCode != 0)
       HandleFailure(result.StandardError);
   ```

5. **Working Directory**: Set the working directory to avoid path resolution issues:
   ```csharp
   var options = new ProcessExecutorOptions
   {
       WorkingDirectory = projectPath
   };
   ```

6. **Environment Variables**: Use environment variables for tools that require special configuration:
   ```csharp
   EnvironmentVariables = new Dictionary<string, string>
   {
       ["PATH"] = $@"{customToolPath};{Environment.GetEnvironmentVariable("PATH")}"
   }
   ```

7. **Output Capture**: Disable output capture for very long-running processes to save memory:
   ```csharp
   var options = new ProcessExecutorOptions
   {
       CaptureOutput = false  // Reduces memory usage
   };
   ```

8. **Process Piping**: Use piping for efficient data transfer between processes without temporary files.

9. **Debug Output**: All output is written to `Debug.WriteLine`, making it visible in debug builds.

10. **Async All the Way**: Always await the async methods to avoid blocking threads:
    ```csharp
    // Good
    var result = await ProcessExecutor.ExecuteAsync(...);

    // Bad - blocks thread
    var result = ProcessExecutor.ExecuteAsync(...).Result;
    ```

11. **Exit Code Handling**: Different tools use different exit codes. Check documentation:
    ```csharp
    if (result.ExitCode == 2)  // Some tools use 2 for warnings
        LogWarning(result.StandardError);
    else if (result.ExitCode != 0)
        throw new Exception("Process failed");
    ```

12. **Progress Accuracy**: For tools that don't report progress linearly, consider implementing custom percentage calculations based on expected duration or file sizes.
