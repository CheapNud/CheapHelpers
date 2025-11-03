# CheapHelpers.Blazor - DownloadHelper

Comprehensive guide to client-side file downloads in Blazor Server applications.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Basic Usage](#basic-usage)
- [Features](#features)
- [API Reference](#api-reference)
- [Advanced Examples](#advanced-examples)
- [Troubleshooting](#troubleshooting)

---

## Overview

`DownloadHelper` provides a simple, unified API for downloading files in Blazor Server applications. It wraps the `BlazorDownloadFile` library and adds conveniences for common download scenarios.

**Key Features:**
- Download files from server file system
- Download Base64-encoded data
- Capture HTML elements as images (PNG/JPG)
- Automatic MIME type detection
- Automatic file cleanup
- Toast notifications for errors
- Localization support

---

## Installation

The `DownloadHelper` is included in `CheapHelpers.Blazor`. It requires:

```xml
<PackageReference Include="BlazorDownloadFile" Version="2.4.0.2" />
<PackageReference Include="MimeMapping" Version="..." />
```

These dependencies are automatically included when you install `CheapHelpers.Blazor`.

---

## Basic Usage

### Register Services

```csharp
// Program.cs
builder.Services.AddBlazorDownloadFile(); // From BlazorDownloadFile package

// Or use CheapHelpers complete setup (includes DownloadHelper)
builder.Services.AddCheapHelpersBlazor<ApplicationUser>(options =>
{
    options.EnableFileDownload = true;
});
```

### Inject in Component

```razor
@inject DownloadHelper DownloadHelper

<MudButton OnClick="DownloadFile">Download Report</MudButton>

@code {
    private async Task DownloadFile()
    {
        var filePath = @"C:\Reports\monthly-report.pdf";
        await DownloadHelper.Download(filePath);
    }
}
```

---

## Features

### 1. Download from File System

Download files from the server's file system and optionally delete after download.

```csharp
// Download and keep file
await DownloadHelper.Download(filePath, deleteFile: false);

// Download and delete file (default)
await DownloadHelper.Download(filePath, deleteFile: true);
```

**Automatic Features:**
- MIME type detection from file extension
- File existence validation
- Error handling with toast notifications
- Automatic file cleanup (optional)

---

### 2. Download Base64 Data

Download Base64-encoded data (useful for dynamically generated files).

```csharp
// Base64 data from API, database, etc.
var base64Data = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...";
await DownloadHelper.DownloadBase64("screenshot.png", base64Data);
```

**Use Cases:**
- Generated images
- Dynamically created PDFs
- API responses
- Client-generated content

---

### 3. Capture HTML as PNG

Capture any HTML element as a PNG image.

```razor
<div id="chart-container">
    <!-- Your chart/content here -->
    <ApexChart TItem="SalesData" Options="_chartOptions">
        <ApexPointSeries ... />
    </ApexChart>
</div>

<MudButton OnClick="DownloadChart">Download Chart</MudButton>

@code {
    private async Task DownloadChart()
    {
        await DownloadHelper.DownloadDivAsPng(
            fileName: "sales-chart.png",
            divId: "chart-container",
            width: 1200,
            height: 600);
    }
}
```

**Features:**
- Customizable dimensions
- Transparent background support
- High-quality rendering
- Supports complex HTML/CSS

---

### 4. Capture HTML as JPG

Capture HTML element as a JPG image (smaller file size).

```csharp
await DownloadHelper.DownloadDivAsJpg(
    fileName: "report.jpg",
    divId: "report-container",
    quality: 0.92,      // 0.0 to 1.0 (higher = better quality)
    width: 800,
    height: 600);
```

**When to Use JPG:**
- Smaller file sizes needed
- No transparency required
- Photos/images (not charts with solid backgrounds)

**When to Use PNG:**
- Transparency needed
- Charts with solid backgrounds
- Text clarity is critical
- Lossless quality required

---

### 5. Capture Without Downloading

Get Base64 data without triggering download (for further processing).

```csharp
// Get PNG as Base64
var pngData = await DownloadHelper.CaptureDivAsPng(
    fileName: "chart.png",  // Used for metadata only
    divId: "chart-container",
    width: 800,
    height: 600);

// Now you can:
// - Upload to server
// - Store in database
// - Send via API
// - Show in preview

await UploadToServer(pngData);
```

```csharp
// Get JPG as Base64
var jpgData = await DownloadHelper.CaptureDivAsJpg(
    fileName: "photo.jpg",
    divId: "photo-container",
    quality: 0.85,
    width: 1920,
    height: 1080);
```

---

## API Reference

### Constructor

```csharp
public DownloadHelper(
    IBlazorDownloadFileService download,
    ISnackbar toast,
    IStringLocalizer loc,
    IJSRuntime js)
```

**Dependencies:**
- `IBlazorDownloadFileService` - File download service
- `ISnackbar` - MudBlazor toast notifications
- `IStringLocalizer` - Localization for error messages
- `IJSRuntime` - JavaScript interop for HTML capture

---

### Methods

#### Download

Download a file from the server file system.

```csharp
Task Download(string filePath, bool deleteFile = true)
```

**Parameters:**
- `filePath` - Full path to file on server
- `deleteFile` - Delete file after download (default: true)

**Returns:** `Task`

**Throws:** Exception if file operation fails

**Example:**

```csharp
// Keep file after download
await DownloadHelper.Download(@"C:\Temp\report.pdf", deleteFile: false);

// Delete file after download (default)
await DownloadHelper.Download(@"C:\Temp\temp-export.xlsx");
```

---

#### DownloadBase64

Download Base64-encoded data as a file.

```csharp
Task DownloadBase64(string fileName, string base64)
```

**Parameters:**
- `fileName` - Desired file name (with extension)
- `base64` - Base64 data (with or without data URI prefix)

**Returns:** `Task`

**Throws:** Exception if Base64 decoding fails

**Example:**

```csharp
// With data URI prefix
var data = "data:application/pdf;base64,JVBERi0xLjcK...";
await DownloadHelper.DownloadBase64("invoice.pdf", data);

// Without prefix (will be parsed automatically)
var data = "JVBERi0xLjcK...";
await DownloadHelper.DownloadBase64("document.pdf", data);
```

---

#### CaptureDivAsPng

Capture HTML element as PNG (returns Base64).

```csharp
Task<string> CaptureDivAsPng(
    string fileName,
    string divId,
    int width = 800,
    int height = 600)
```

**Parameters:**
- `fileName` - File name (metadata only, not used for actual file)
- `divId` - HTML element ID to capture
- `width` - Capture width in pixels (default: 800)
- `height` - Capture height in pixels (default: 600)

**Returns:** `Task<string>` - Base64-encoded PNG data

**Example:**

```csharp
var pngData = await DownloadHelper.CaptureDivAsPng(
    "dashboard.png",
    "dashboard-container",
    1920,
    1080);

// pngData contains: "data:image/png;base64,iVBORw0KGgo..."
```

---

#### CaptureDivAsJpg

Capture HTML element as JPG (returns Base64).

```csharp
Task<string> CaptureDivAsJpg(
    string fileName,
    string divId,
    double quality = 0.92,
    int width = 800,
    int height = 600)
```

**Parameters:**
- `fileName` - File name (metadata only)
- `divId` - HTML element ID to capture
- `quality` - JPEG quality (0.0 to 1.0, default: 0.92)
- `width` - Capture width in pixels (default: 800)
- `height` - Capture height in pixels (default: 600)

**Returns:** `Task<string>` - Base64-encoded JPG data

**Example:**

```csharp
var jpgData = await DownloadHelper.CaptureDivAsJpg(
    "report.jpg",
    "report-container",
    quality: 0.85,
    width: 1200,
    height: 800);
```

---

#### DownloadDivAsPng

Capture and download HTML element as PNG.

```csharp
Task DownloadDivAsPng(
    string fileName,
    string divId,
    int width = 800,
    int height = 600)
```

**Parameters:**
- `fileName` - Download file name (with .png extension)
- `divId` - HTML element ID to capture
- `width` - Capture width in pixels (default: 800)
- `height` - Capture height in pixels (default: 600)

**Returns:** `Task`

**Example:**

```csharp
await DownloadHelper.DownloadDivAsPng(
    "quarterly-chart.png",
    "q1-chart",
    1600,
    900);
```

---

#### DownloadDivAsJpg

Capture and download HTML element as JPG.

```csharp
Task DownloadDivAsJpg(
    string fileName,
    string divId,
    double quality = 0.92,
    int width = 800,
    int height = 600)
```

**Parameters:**
- `fileName` - Download file name (with .jpg extension)
- `divId` - HTML element ID to capture
- `quality` - JPEG quality (0.0 to 1.0, default: 0.92)
- `width` - Capture width in pixels (default: 800)
- `height` - Capture height in pixels (default: 600)

**Returns:** `Task`

**Example:**

```csharp
await DownloadHelper.DownloadDivAsJpg(
    "product-photo.jpg",
    "photo-preview",
    quality: 0.90,
    width: 1920,
    height: 1080);
```

---

## Advanced Examples

### Export Report with Progress

```razor
@inject DownloadHelper DownloadHelper
@inject ISnackbar Snackbar

<MudButton OnClick="ExportReport"
           Disabled="_isExporting">
    @if (_isExporting)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
        <span>Exporting...</span>
    }
    else
    {
        <span>Export Report</span>
    }
</MudButton>

@code {
    private bool _isExporting;

    private async Task ExportReport()
    {
        _isExporting = true;

        try
        {
            // Generate report
            var reportPath = await GenerateReportAsync();

            // Download and delete
            await DownloadHelper.Download(reportPath, deleteFile: true);

            Snackbar.Add("Report downloaded successfully", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isExporting = false;
        }
    }

    private async Task<string> GenerateReportAsync()
    {
        // Your report generation logic
        await Task.Delay(2000); // Simulate generation
        return @"C:\Temp\report-2024.pdf";
    }
}
```

---

### Batch Download Multiple Files

```csharp
private async Task DownloadAllReports()
{
    var reportFiles = new[]
    {
        @"C:\Reports\january.pdf",
        @"C:\Reports\february.pdf",
        @"C:\Reports\march.pdf"
    };

    foreach (var file in reportFiles)
    {
        try
        {
            await DownloadHelper.Download(file, deleteFile: false);

            // Small delay between downloads to prevent browser blocking
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to download {file}: {ex.Message}");
        }
    }
}
```

---

### Capture Multiple Charts

```razor
<div id="chart-sales">
    <ApexChart ... />
</div>

<div id="chart-revenue">
    <ApexChart ... />
</div>

<div id="chart-customers">
    <ApexChart ... />
</div>

<MudButton OnClick="DownloadAllCharts">Download All Charts</MudButton>

@code {
    private async Task DownloadAllCharts()
    {
        var charts = new[]
        {
            ("chart-sales", "sales-chart.png"),
            ("chart-revenue", "revenue-chart.png"),
            ("chart-customers", "customers-chart.png")
        };

        foreach (var (divId, fileName) in charts)
        {
            await DownloadHelper.DownloadDivAsPng(fileName, divId, 1200, 600);
            await Task.Delay(500); // Prevent browser blocking
        }

        Snackbar.Add("All charts downloaded", Severity.Success);
    }
}
```

---

### Dynamic File Generation and Download

```csharp
private async Task GenerateAndDownloadInvoice(int invoiceId)
{
    // Generate PDF in memory
    var pdfBytes = await _invoiceService.GeneratePdfAsync(invoiceId);

    // Convert to Base64
    var base64 = Convert.ToBase64String(pdfBytes);
    var dataUri = $"data:application/pdf;base64,{base64}";

    // Download
    await DownloadHelper.DownloadBase64($"invoice-{invoiceId}.pdf", dataUri);
}
```

---

### Capture with Custom Styling

```razor
<style>
    #export-container {
        background: white;
        padding: 40px;
        border: 1px solid #ddd;
    }

    .print-only {
        display: none;
    }

    @media print {
        .print-only {
            display: block;
        }
    }
</style>

<div id="export-container">
    <h1>Sales Report</h1>
    <MudTable Items="_salesData">
        <!-- Table content -->
    </MudTable>

    <div class="print-only">
        <p>Generated: @DateTime.Now</p>
    </div>
</div>

<MudButton OnClick="ExportReport">Export as Image</MudButton>

@code {
    private async Task ExportReport()
    {
        // Temporarily add print-only styles
        await JS.InvokeVoidAsync("eval",
            "document.querySelectorAll('.print-only').forEach(el => el.style.display = 'block')");

        // Capture
        await DownloadHelper.DownloadDivAsPng(
            "sales-report.png",
            "export-container",
            1600,
            1200);

        // Remove print-only styles
        await JS.InvokeVoidAsync("eval",
            "document.querySelectorAll('.print-only').forEach(el => el.style.display = 'none')");
    }
}
```

---

### Upload Captured Image

```csharp
private async Task CaptureAndUpload()
{
    // Capture chart as Base64
    var chartData = await DownloadHelper.CaptureDivAsPng(
        "chart.png",
        "chart-container",
        800,
        600);

    // Parse Base64 (remove data URI prefix)
    var base64 = chartData.Split(",")[1];
    var bytes = Convert.FromBase64String(base64);

    // Upload to server
    using var content = new ByteArrayContent(bytes);
    content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

    var response = await _http.PostAsync("/api/charts/upload", content);

    if (response.IsSuccessStatusCode)
    {
        Snackbar.Add("Chart uploaded successfully", Severity.Success);
    }
}
```

---

## Troubleshooting

### Files Not Downloading

**Issue:** Files download but don't save.

**Solution:** Check browser settings. Some browsers block multiple downloads. Add delay between downloads:

```csharp
await Task.Delay(500); // Between downloads
```

---

### HTML Capture Shows Wrong Size

**Issue:** Captured image is cropped or wrong dimensions.

**Solution:** Ensure element has explicit size or use larger capture dimensions:

```csharp
// Make sure div has explicit dimensions
<div id="chart" style="width: 800px; height: 600px;">
    ...
</div>

// Or use larger capture size
await DownloadHelper.DownloadDivAsPng("chart.png", "chart", 1600, 1200);
```

---

### Transparent Background Shows Black

**Issue:** PNG transparency shows as black background.

**Solution:** This is expected behavior. Add white background if needed:

```csharp
// Using JPG with white background
await DownloadHelper.DownloadDivAsJpg("chart.jpg", "chart", 0.92, 800, 600);
```

---

### Memory Issues with Large Files

**Issue:** Out of memory errors with large files.

**Solution:** Use file streaming instead of Base64 for very large files:

```csharp
// For large files, use direct file download
await DownloadHelper.Download(filePath); // Streams file

// Avoid Base64 for files > 10MB
```

---

### MIME Type Incorrect

**Issue:** Downloaded file has wrong MIME type.

**Solution:** Ensure file has correct extension. `MimeUtility.GetMimeMapping()` uses extension to determine MIME type:

```csharp
// Correct
await DownloadHelper.Download("report.pdf");

// Wrong extension = wrong MIME type
await DownloadHelper.Download("report.txt"); // Will be text/plain
```

---

### JavaScript Errors in Console

**Issue:** Console shows errors about `htmlToPng` or `htmlToJpg` not found.

**Solution:** Ensure JavaScript files are referenced:

```html
<!-- _Host.cshtml or App.razor -->
<script src="_content/CheapHelpers.Blazor/js/site.js"></script>
```

If custom implementation needed, add to `site.js`:

```javascript
// wwwroot/js/site.js
window.htmlToPng = async function(elementId, options) {
    const element = document.getElementById(elementId);
    return await html2canvas(element, options).then(canvas =>
        canvas.toDataURL('image/png'));
};

window.htmlToJpg = async function(elementId, options, quality) {
    const element = document.getElementById(elementId);
    return await html2canvas(element, options).then(canvas =>
        canvas.toDataURL('image/jpeg', quality));
};
```

Requires `html2canvas` library:

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js"></script>
```

---

## See Also

- [Components.md](Components.md) - Blazor UI components
- [ClipboardService.md](ClipboardService.md) - Clipboard operations
- [Hybrid.md](Hybrid.md) - Blazor Hybrid features
