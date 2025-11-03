# FileHelper

Secure filename generation with GUID suffixes and date-based naming patterns.

## Overview

The `FileHelper` class provides methods for generating secure filenames that prevent file overwrite attacks by appending GUID suffixes. It also includes specialized methods for date-based filename generation (daily, weekly, monthly, yearly patterns).

## Namespace

```csharp
using CheapHelpers.Helpers.Files;
```

## Core Methods

### GetTrustedFileName

Generates a secure filename with GUID suffix to prevent file overwrite attacks. Optionally includes a formatted date/time pattern.

**Signature:**
```csharp
public static string GetTrustedFileName(
    string filename,
    DateTime? timestamp = null,
    string? dateFormat = null)
```

**Parameters:**
- `filename`: The filename (with or without extension)
- `timestamp`: Optional timestamp to include in the filename
- `dateFormat`: Optional date format pattern (e.g., "yyyy-MM-dd")

**Returns:** Secure filename with format: `{filename}[_{formattedDate}]_{guid}[.{extension}]`

**Throws:** `ArgumentException` if filename is null or whitespace

**Example:**
```csharp
// Simple usage - adds GUID only
string secure = FileHelper.GetTrustedFileName("document.pdf");
// Result: "document_a1b2c3d4.pdf"

// With timestamp and default format
string dated = FileHelper.GetTrustedFileName(
    "report.xlsx",
    DateTime.Now);
// Result: "report_1/15/2024 2:30:00 PM_a1b2c3d4.xlsx"

// With custom date format
string custom = FileHelper.GetTrustedFileName(
    "backup.zip",
    DateTime.Now,
    "yyyy-MM-dd_HHmmss");
// Result: "backup_2024-01-15_143000_a1b2c3d4.zip"
```

### GetTrustedFileName (FileInfo)

Generates a secure filename from a FileInfo object.

**Signature:**
```csharp
public static string GetTrustedFileName(FileInfo file)
```

**Parameters:**
- `file`: FileInfo object

**Returns:** Secure filename with GUID suffix

**Example:**
```csharp
var fileInfo = new FileInfo(@"C:\temp\upload.jpg");
string secure = FileHelper.GetTrustedFileName(fileInfo);
// Result: "upload_a1b2c3d4.jpg"
```

### GetTrustedFileNameFromPath

Generates a secure filename from a file path.

**Signature:**
```csharp
public static string GetTrustedFileNameFromPath(string filepath)
```

**Parameters:**
- `filepath`: Full path to the file

**Returns:** Secure filename with GUID suffix

**Example:**
```csharp
string secure = FileHelper.GetTrustedFileNameFromPath(@"C:\temp\file.txt");
// Result: "file_a1b2c3d4.txt"
```

### GetTrustedFileNameFromTempPath

Generates a secure filename from a filename in the temp directory.

**Signature:**
```csharp
public static string GetTrustedFileNameFromTempPath(string filename)
```

**Parameters:**
- `filename`: Filename in temp directory

**Returns:** Secure filename with GUID suffix

**Example:**
```csharp
string secure = FileHelper.GetTrustedFileNameFromTempPath("temp_file.dat");
// Result: "temp_file_a1b2c3d4.dat"
```

### ChangeFileNameId

Regenerates the GUID suffix for a filename.

**Signature:**
```csharp
public static string ChangeFileNameId(string filename)
```

**Parameters:**
- `filename`: Existing filename with GUID suffix

**Returns:** Same filename with new GUID suffix

**Example:**
```csharp
string original = "document_a1b2c3d4.pdf";
string newName = FileHelper.ChangeFileNameId(original);
// Result: "document_e5f6g7h8.pdf"
```

## Date-Based Filename Methods

### GetDailyFilename

Generates a secure filename with daily pattern (yyyy-MM-dd).

**Signature:**
```csharp
public static string GetDailyFilename(DateTime timestamp, string filename)
public static string GetDailyFilename(DateTimeOffset timestamp, string filename)
```

**Parameters:**
- `timestamp`: The timestamp to use for the filename
- `filename`: The filename (with or without extension)

**Returns:** Secure filename in format: `{filename}_yyyy-MM-dd_{guid}[.{extension}]`

**Example:**
```csharp
var date = new DateTime(2024, 1, 15);
string daily = FileHelper.GetDailyFilename(date, "backup.sql");
// Result: "backup_2024-01-15_a1b2c3d4.sql"
```

### GetWeeklyFilename

Generates a secure filename with weekly pattern (yyyy-wN where N is week number).

**Signature:**
```csharp
public static string GetWeeklyFilename(DateTime timestamp, string filename)
public static string GetWeeklyFilename(DateTimeOffset timestamp, string filename)
```

**Parameters:**
- `timestamp`: The timestamp to use for the filename
- `filename`: The filename (with or without extension)

**Returns:** Secure filename in format: `{filename}_yyyy-wN_{guid}[.{extension}]`

**Example:**
```csharp
var date = new DateTime(2024, 1, 15);  // Week 3 of 2024
string weekly = FileHelper.GetWeeklyFilename(date, "report.xlsx");
// Result: "report_2024-w3_a1b2c3d4.xlsx"
```

### GetMonthlyFilename

Generates a secure filename with monthly pattern (yyyy-MM).

**Signature:**
```csharp
public static string GetMonthlyFilename(DateTime timestamp, string filename)
public static string GetMonthlyFilename(DateTimeOffset timestamp, string filename)
```

**Parameters:**
- `timestamp`: The timestamp to use for the filename
- `filename`: The filename (with or without extension)

**Returns:** Secure filename in format: `{filename}_yyyy-MM_{guid}[.{extension}]`

**Example:**
```csharp
var date = new DateTime(2024, 1, 15);
string monthly = FileHelper.GetMonthlyFilename(date, "summary.pdf");
// Result: "summary_2024-01_a1b2c3d4.pdf"
```

### GetYearlyFilename

Generates a secure filename with yearly pattern (yyyy).

**Signature:**
```csharp
public static string GetYearlyFilename(DateTime timestamp, string filename)
public static string GetYearlyFilename(DateTimeOffset timestamp, string filename)
```

**Parameters:**
- `timestamp`: The timestamp to use for the filename
- `filename`: The filename (with or without extension)

**Returns:** Secure filename in format: `{filename}_yyyy_{guid}[.{extension}]`

**Example:**
```csharp
var date = new DateTime(2024, 1, 15);
string yearly = FileHelper.GetYearlyFilename(date, "annual_report.docx");
// Result: "annual_report_2024_a1b2c3d4.docx"
```

### GetCustomDateFilename

Generates a secure custom date-based filename with specified format pattern.

**Signature:**
```csharp
public static string GetCustomDateFilename(
    DateTime timestamp,
    string filename,
    string dateFormat)

public static string GetCustomDateFilename(
    DateTimeOffset timestamp,
    string filename,
    string dateFormat)
```

**Parameters:**
- `timestamp`: The timestamp to use for the filename
- `filename`: The filename (with or without extension)
- `dateFormat`: Custom date format pattern (e.g., "yyyyMMdd", "yyyy-MM-dd-HHmmss")

**Returns:** Secure filename in format: `{filename}_{datePattern}_{guid}[.{extension}]`

**Example:**
```csharp
var timestamp = new DateTime(2024, 1, 15, 14, 30, 0);

// Date only
string dateOnly = FileHelper.GetCustomDateFilename(
    timestamp,
    "log.txt",
    "yyyyMMdd");
// Result: "log_20240115_a1b2c3d4.txt"

// Date and time
string dateTime = FileHelper.GetCustomDateFilename(
    timestamp,
    "snapshot.json",
    "yyyy-MM-dd_HH-mm-ss");
// Result: "snapshot_2024-01-15_14-30-00_a1b2c3d4.json"
```

## Common Use Cases

### Secure File Uploads

```csharp
public class FileUploadService
{
    public async Task<string> SaveUploadedFile(IFormFile file, string uploadPath)
    {
        // Generate secure filename to prevent overwrite attacks
        string secureFilename = FileHelper.GetTrustedFileName(file.FileName);
        string fullPath = Path.Combine(uploadPath, secureFilename);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return secureFilename;
    }
}
```

### Daily Backup Files

```csharp
public class BackupService
{
    private readonly string _backupPath = @"C:\Backups";

    public async Task CreateDailyBackup()
    {
        var today = DateTime.Today;
        string filename = FileHelper.GetDailyFilename(today, "database_backup.bak");
        string fullPath = Path.Combine(_backupPath, filename);

        await PerformBackup(fullPath);

        // Result: C:\Backups\database_backup_2024-01-15_a1b2c3d4.bak
    }
}
```

### Periodic Report Generation

```csharp
public class ReportGenerator
{
    public async Task GenerateWeeklyReport()
    {
        var reportDate = DateTime.Today;
        string filename = FileHelper.GetWeeklyFilename(
            reportDate,
            "sales_report.xlsx");

        await GenerateExcelReport(filename);

        // Result: sales_report_2024-w3_a1b2c3d4.xlsx
    }

    public async Task GenerateMonthlyReport()
    {
        var reportDate = DateTime.Today;
        string filename = FileHelper.GetMonthlyFilename(
            reportDate,
            "monthly_summary.pdf");

        await GeneratePdfReport(filename);

        // Result: monthly_summary_2024-01_a1b2c3d4.pdf
    }
}
```

### Log File Rotation

```csharp
public class LogRotationService
{
    private readonly string _logDirectory = @"C:\Logs";

    public string GetCurrentLogFile()
    {
        var now = DateTime.Now;
        string filename = FileHelper.GetCustomDateFilename(
            now,
            "application.log",
            "yyyy-MM-dd_HH");

        // New log file each hour
        return Path.Combine(_logDirectory, filename);
        // Result: C:\Logs\application_2024-01-15_14_a1b2c3d4.log
    }

    public string GetDailyLogFile()
    {
        string filename = FileHelper.GetDailyFilename(
            DateTime.Today,
            "app.log");

        return Path.Combine(_logDirectory, filename);
        // Result: C:\Logs\app_2024-01-15_a1b2c3d4.log
    }
}
```

### Export File Naming

```csharp
public class DataExportService
{
    public async Task<string> ExportUserData(int userId)
    {
        var exportTime = DateTime.Now;

        string filename = FileHelper.GetCustomDateFilename(
            exportTime,
            $"user_{userId}_export.csv",
            "yyyyMMdd_HHmmss");

        await GenerateExport(userId, filename);

        return filename;
        // Result: user_123_export_20240115_143000_a1b2c3d4.csv
    }
}
```

### Archival System

```csharp
public class ArchiveManager
{
    private readonly string _archiveRoot = @"C:\Archives";

    public async Task ArchiveDocument(string originalFile)
    {
        var archiveDate = DateTime.Today;

        // Organize by year, then month
        var yearFolder = archiveDate.Year.ToString();
        var monthFolder = archiveDate.ToString("MM");

        string secureFilename = FileHelper.GetDailyFilename(
            archiveDate,
            Path.GetFileName(originalFile));

        string archivePath = Path.Combine(
            _archiveRoot,
            yearFolder,
            monthFolder,
            secureFilename);

        Directory.CreateDirectory(Path.GetDirectoryName(archivePath));
        await File.CopyAsync(originalFile, archivePath);

        // Result: C:\Archives\2024\01\document_2024-01-15_a1b2c3d4.pdf
    }
}
```

### Temporary File Management

```csharp
public class TempFileService
{
    public string CreateTempFile(string baseFilename)
    {
        string secureFilename = FileHelper.GetTrustedFileNameFromTempPath(
            baseFilename);

        string fullPath = Path.Combine(
            Path.GetTempPath(),
            secureFilename);

        File.Create(fullPath).Close();

        return fullPath;
    }

    public void CleanupOldTempFiles()
    {
        var tempPath = Path.GetTempPath();
        var files = Directory.GetFiles(tempPath, "*_????????.*");

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTime < DateTime.Now.AddDays(-7))
            {
                File.Delete(file);
            }
        }
    }
}
```

## Tips and Best Practices

1. **Security**: The GUID suffix (8 characters from a new GUID) makes filenames unpredictable and prevents:
   - File overwrite attacks
   - Filename collision
   - Predictable file enumeration

2. **GUID Format**: The GUID is generated with `.ToString("N")[..8]`, producing 8 hexadecimal characters without hyphens.

3. **Extension Preservation**: All methods preserve the original file extension, maintaining file type associations.

4. **Date Formats**:
   - **Daily**: `yyyy-MM-dd` - ISO 8601 compliant, sortable
   - **Weekly**: `yyyy-wN` - Year and week number
   - **Monthly**: `yyyy-MM` - Year and month
   - **Yearly**: `yyyy` - Year only
   - **Custom**: Any valid DateTime format string

5. **Path Handling**: Methods handle filenames with or without extensions gracefully. Always use `Path.Combine` when building full paths.

6. **Collision Probability**: With 8 hex characters, there are 4.3 billion possible combinations, making collisions extremely unlikely for typical use cases.

7. **Sorting**: Date-based filenames with ISO format (yyyy-MM-dd) sort correctly alphabetically and chronologically.

8. **File Organization**: Combine date patterns with directory structures:
   ```csharp
   var year = date.Year.ToString();
   var month = date.ToString("MM");
   var filename = FileHelper.GetDailyFilename(date, "data.json");
   var fullPath = Path.Combine(baseDir, year, month, filename);
   ```

9. **Validation**: While the methods validate input, always validate user-provided filenames before passing to these methods:
   ```csharp
   string safeFilename = Path.GetFileName(userInput); // Remove path traversal
   string secure = FileHelper.GetTrustedFileName(safeFilename);
   ```

10. **Cleanup**: When using GUID-suffixed files, implement cleanup based on creation date rather than filename patterns, as GUIDs are random.

11. **Database Storage**: Store the secure filename in your database to retrieve files later:
    ```csharp
    var secureFilename = FileHelper.GetTrustedFileName(originalFilename);
    fileRecord.StoredFilename = secureFilename;
    fileRecord.OriginalFilename = originalFilename;
    ```

12. **URL Safe**: The generated filenames are URL-safe (alphanumeric + underscore + dash + period), making them suitable for web applications without additional encoding.
