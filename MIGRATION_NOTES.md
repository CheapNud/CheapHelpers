# CheapHelpers - Migration Notes

## Overview

This document describes the utilities extracted from ShotcutRandomizer to CheapHelpers and provides migration guidance.

## Extracted Utilities

### 1. TemporaryFileManager

**Location:** `C:\Users\Brech\source\repos\CheapHelpers\CheapHelpers\IO\TemporaryFileManager.cs`

**Source:** `C:\Users\Brech\source\repos\ShotcutRandomizer\Services\Utilities\TemporaryFileManager.cs`

**Namespace Change:**
- **Old:** `CheapShotcutRandomizer.Services.Utilities`
- **New:** `CheapHelpers.IO`

**Status:** COMPLETED - Ready for use

### 2. ProcessExecutor (NEW)

**Location:** `C:\Users\Brech\source\repos\CheapHelpers\CheapHelpers\Process\ProcessExecutor.cs`

**Namespace:** `CheapHelpers.Process`

**Status:** COMPLETED - Ready for use

**Features:**
- Basic process execution with progress tracking
- Process piping (stdout -> stdin)
- Timeout handling
- Cancellation support
- Regex-based progress extraction
- Predefined patterns for common progress formats

## Migration Steps for ShotcutRandomizer

### Step 1: Add CheapHelpers Reference

Add a project reference to CheapHelpers in ShotcutRandomizer.csproj:

```xml
<ItemGroup>
  <ProjectReference Include="..\CheapHelpers\CheapHelpers\CheapHelpers.csproj" />
</ItemGroup>
```

### Step 2: Update Using Statements

Replace the old namespace with the new one in all files using TemporaryFileManager:

**Before:**
```csharp
using CheapShotcutRandomizer.Services.Utilities;
```

**After:**
```csharp
using CheapHelpers.IO;
```

### Step 3: Remove Old File

Delete the old TemporaryFileManager.cs from ShotcutRandomizer:

```
C:\Users\Brech\source\repos\ShotcutRandomizer\Services\Utilities\TemporaryFileManager.cs
```

### Step 4: Verify Build

Build ShotcutRandomizer to ensure all references are updated:

```bash
dotnet build ShotcutRandomizer.csproj
```

## Files Using TemporaryFileManager in ShotcutRandomizer

The following files will need their using statements updated:

1. Any service that creates temporary files
2. RifeVideoProcessingPipeline (likely uses temp files)
3. FFmpegRenderService (likely uses temp files)

Use the following grep command to find all usages:

```bash
grep -r "TemporaryFileManager" --include="*.cs" .
```

## Future Migration Opportunities

### Files that COULD Use ProcessExecutor

The following patterns in ShotcutRandomizer could be refactored to use ProcessExecutor:

1. **RifeInterpolationService.cs (Lines 394-462)**
   - Current: Manual process execution with progress tracking
   - Benefit: Simplified code, consistent error handling
   - Effort: Low

2. **RifeInterpolationService.cs (Lines 559-594)**
   - Current: Manual FFmpeg process with timeout
   - Benefit: Built-in timeout handling, better logging
   - Effort: Low

3. **RifeInterpolationService.cs (Lines 274-327)**
   - Current: Manual process piping (vspipe -> ffmpeg)
   - Benefit: Use ExecuteWithPipingAsync, cleaner code
   - Effort: Medium

4. **FFmpegRenderService.cs**
   - Current: Likely has manual FFmpeg execution
   - Benefit: Standardized progress tracking
   - Effort: Low-Medium

### Migration Priority

**High Priority (Recommend now):**
- TemporaryFileManager migration (blocking further development)

**Medium Priority (Next iteration):**
- Simple process executions without piping
- FFmpegRenderService standardization

**Low Priority (Future cleanup):**
- Complex process piping scenarios
- VapourSynth script execution

## Testing Checklist

After migration, verify:

- [ ] All temporary files are created and cleaned up correctly
- [ ] No lingering using statements for old namespace
- [ ] Build succeeds with no errors
- [ ] All services using TemporaryFileManager work as expected
- [ ] Temp file cleanup occurs on exceptions
- [ ] Temp file cleanup occurs on cancellation

## Notes

- TemporaryFileManager is a direct copy with only namespace changes
- ProcessExecutor is a new utility consolidating patterns from ShotcutRandomizer
- Both utilities use Debug.WriteLine for diagnostics (as per project standards)
- Both follow C# 13 best practices (collection expressions, primary constructors where appropriate)
