# Migration Guide: CheapHelpers 1.x → 2.0

This guide helps you migrate from CheapHelpers 1.x to 2.0.

## Breaking Changes

### FileHelper.GetTrustedFileName() - Signature Change

**Impact:** High - Method signature changed

**Old Behavior (1.x):**
```csharp
public static string GetTrustedFileName(string filename)

// Usage:
var trusted = FileHelper.GetTrustedFileName("myfile.txt");
// Returns: "myfile_a1b2c3d4.txt"
```

**New Behavior (2.0):**
```csharp
public static string GetTrustedFileName(
    string filename,
    DateTime? timestamp = null,
    string? dateFormat = null)

// Usage:
var trusted = FileHelper.GetTrustedFileName("myfile.txt");
// Returns: "myfile_a1b2c3d4.txt" (same result, extension automatically extracted)
```

**Migration Steps:**

1. **If you're passing a filename with extension** (most common case):
   ```csharp
   // OLD (1.x)
   var result = FileHelper.GetTrustedFileName("document.pdf");

   // NEW (2.0) - No change needed, still works!
   var result = FileHelper.GetTrustedFileName("document.pdf");
   // The method auto-extracts the extension
   ```

2. **If you want to add a timestamp** (new feature):
   ```csharp
   // NEW (2.0) - Add date to filename
   var result = FileHelper.GetTrustedFileName(
       "backup.dat",
       DateTime.Now,
       "yyyy-MM-dd");
   // Returns: "backup_2025-01-15_a1b2c3d4.dat"
   ```

3. **If you're using other GetTrustedFileName overloads:**
   ```csharp
   // These remain unchanged:
   FileHelper.GetTrustedFileName(FileInfo file)
   FileHelper.GetTrustedFileNameFromPath(string filepath)
   FileHelper.GetTrustedFileNameFromTempPath(string filename)
   ```

**Why This Changed:**

The method was enhanced to support date-based filename generation while maintaining backward compatibility. The method intelligently extracts the extension from the filename parameter, eliminating the need for separate baseName and extension handling.

## New Features in 2.0

### Date-Based Filename Generators

New methods for generating timestamped filenames:

```csharp
// Daily: backup_2025-01-15_a1b2c3d4.dat
FileHelper.GetDailyFilename(DateTime.Now, "backup.dat");

// Weekly: report_2025-w3_a1b2c3d4.csv
FileHelper.GetWeeklyFilename(DateTime.Now, "report.csv");

// Monthly: summary_2025-01_a1b2c3d4.json
FileHelper.GetMonthlyFilename(DateTime.Now, "summary.json");

// Yearly: archive_2025_a1b2c3d4.zip
FileHelper.GetYearlyFilename(DateTime.Now, "archive.zip");

// Custom format: log_20250115-143022_a1b2c3d4.txt
FileHelper.GetCustomDateFilename(DateTime.Now, "log.txt", "yyyyMMdd-HHmmss");
```

### Dynamic LINQ Ordering

Replaced enum-based pattern with cleaner method names:

```csharp
// Sort ascending
var sorted = query.OrderByDynamic("PropertyName");

// Sort descending
var sorted = query.OrderByDescendingDynamic("PropertyName");
```

### DateTimeOffset Extensions

New rounding methods for DateTimeOffset:

```csharp
var timestamp = DateTimeOffset.Now;

// Round to intervals
timestamp.Floor(TimeSpan.FromMinutes(15));    // Round down
timestamp.Round(TimeSpan.FromMinutes(15));    // Round nearest
timestamp.Ceiling(TimeSpan.FromMinutes(15));  // Round up

// Truncate to specific precision
timestamp.PerMinute();  // Zero out seconds
timestamp.PerHour();    // Zero out minutes and seconds
timestamp.PerDay();     // Zero out time component
timestamp.ToZeroTime(); // Convert to midnight UTC
```

### String Sanitization

New string cleaning methods:

```csharp
// Remove all special characters
"Hello@World!".RemoveSpecialCharacters();  // "HelloWorld"

// Keep dashes, collapse multiples
"Hello--World".RemoveSpecialCharactersKeepDash();  // "Hello-World"

// Sanitize for filenames/URLs
"My File/Name".Sanitize();  // "My_File-Name"

// Trim with ellipsis
"Very long text here".TrimWithEllipsis(10);  // "Very long ..."
```

### Additional New Utilities

- **BinaryReaderWriterExtensions**: Read/Write DateTimeOffset
- **BitHelper**: Bit manipulation and endian conversions
- **HashHelper**: MD5 and FNV hashing
- **TimeSpanExtensions**: Human-readable formatting
- **TypeExtensions**: Cached attribute reflection
- **UriExtensions**: URL base extraction

## Removed Features

None. This release is additive with one signature enhancement (GetTrustedFileName).

## Upgrade Checklist

- [ ] Update NuGet package to 2.0.0
- [ ] Review any custom calls to `FileHelper.GetTrustedFileName()`
- [ ] Test your application thoroughly
- [ ] Consider using new date-based filename generators
- [ ] Update any Order enum usage to new OrderByDynamic methods

## Questions?

If you encounter issues during migration, please open an issue on GitHub:
https://github.com/CheapNud/CheapHelpers/issues
