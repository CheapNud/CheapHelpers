# CheapHelpers

A collection of C# utility classes and extension methods for common development tasks.

## Latest Additions

### IO Utilities

#### TemporaryFileManager
Manages temporary files and directories with automatic cleanup. Ensures proper cleanup even on exceptions or cancellation.

**Features:**
- Automatic cleanup via IDisposable pattern
- Track multiple directories and files
- Get directory size with human-readable formatting
- Exception-safe cleanup

See [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md#temporaryfilemanager) for detailed examples.

### Process Utilities

#### ProcessExecutor
Generic process execution wrapper with progress tracking, timeout handling, and cancellation support.

**Features:**
- Basic process execution with configurable options
- Process piping (stdout to stdin)
- Regex-based progress extraction with predefined patterns
- Timeout handling with automatic cleanup
- Full cancellation support
- Captured output (stdout/stderr)
- Environment variable configuration

See [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md#processexecutor) for detailed examples.

## Documentation

- [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) - Comprehensive usage examples for all utilities
- [MIGRATION_NOTES.md](MIGRATION_NOTES.md) - Migration guide from ShotcutRandomizer
- [REFACTORING_OPPORTUNITIES.md](REFACTORING_OPPORTUNITIES.md) - Identified refactoring opportunities in ShotcutRandomizer

## Installation

Add a project reference to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\CheapHelpers\CheapHelpers\CheapHelpers.csproj" />
</ItemGroup>
```

## Quick Start

### TemporaryFileManager

```csharp
using CheapHelpers.IO;

using var tempManager = new TemporaryFileManager();
var workDir = tempManager.CreateTempDirectory("processing");
var outputFile = tempManager.CreateTempFile("output.mp4");

// Use the paths...
// Cleanup happens automatically on dispose
```

### ProcessExecutor

```csharp
using CheapHelpers.Process;

var result = await ProcessExecutor.ExecuteAsync(
    executable: "ffmpeg",
    arguments: "-i input.mp4 output.mp4",
    options: new ProcessExecutorOptions
    {
        Timeout = TimeSpan.FromMinutes(10),
        ProgressPatterns = ProgressPattern.GetCommonPatterns()
    },
    progress: new Progress<ProcessProgress>(p =>
        Debug.WriteLine($"Progress: {p.Percentage:F2}%")));

if (result.Success)
{
    Debug.WriteLine($"Completed in {result.Duration}");
}
```

## Requirements

- .NET 9.0
- C# 13

## License

See [LICENSE.txt](LICENSE.txt)