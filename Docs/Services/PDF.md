# PDF Services

Comprehensive PDF generation, templating, and optimization services using iText and iLovePDF.

## Table of Contents

- [Overview](#overview)
- [Available Services](#available-services)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [PDF Templates](#pdf-templates)
- [Optimization](#optimization)
- [Dependency Injection Setup](#dependency-injection-setup)
- [Common Scenarios](#common-scenarios)

## Overview

The CheapHelpers.Services PDF package provides complete PDF capabilities:

1. **PDF Generation** - Create PDFs from data with templates
2. **PDF Optimization** - Reduce file size with iText or iLovePDF
3. **PDF Templates** - Flexible template system for reports and documents

### Key Features

- Template-based PDF generation using iText
- Data-driven tables and sections
- Header and footer support
- Color scheme presets
- Two-tier optimization (iLovePDF with iText fallback)
- Stream and file-based operations
- Multi-column layouts
- Custom formatters for data display
- Automatic page numbering

## Available Services

### IPdfService

Main interface for PDF operations:

```csharp
public interface IPdfService
{
    void Optimize(string source, string destination);
    void Optimize(Stream source, Stream destination);
    Task<PdfOptimizationResult> OptimizeAsync(string source, string destination,
        PdfOptimizationLevel level = PdfOptimizationLevel.Balanced);
    Task<PdfOptimizationResult> OptimizeAsync(Stream source, Stream destination,
        PdfOptimizationLevel level = PdfOptimizationLevel.Balanced);
}
```

### IPdfExportService

Export data to PDF using templates:

```csharp
public interface IPdfExportService
{
    Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template);
    Task ExportToPdfFileAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, string filePath);
    Task<byte[]> ExportSingleToPdfAsync<T>(T entity, PdfDocumentTemplate template);
}
```

### IPdfOptimizationService

Lower-level optimization control:

```csharp
public interface IPdfOptimizationService
{
    Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(string inputPath, string outputPath,
        PdfOptimizationLevel level);
    Task<PdfOptimizationResult> OptimizeWithILovePdfAsync(Stream input, Stream output,
        PdfOptimizationLevel level);
    PdfOptimizationResult OptimizeWithIText(string inputPath, string outputPath,
        PdfOptimizationLevel level);
    PdfOptimizationResult OptimizeWithIText(Stream input, Stream output,
        PdfOptimizationLevel level);
    bool IsILovePdfAvailable { get; }
}
```

### IPdfTemplateService

Build PDF templates programmatically:

```csharp
public interface IPdfTemplateService
{
    // Methods for building templates
}
```

## Configuration

### Service Registration

```csharp
services.AddScoped<IPdfExportService, PdfExportService>();
services.AddScoped<IPdfTemplateService, PdfTemplateService>();
services.AddScoped<IPdfOptimizationService, PdfOptimizationService>();
services.AddScoped<IPdfService, PdfTextService>();
```

### Optimization Levels

```csharp
public enum PdfOptimizationLevel
{
    Low,       // Minimal compression, faster processing
    Balanced,  // Default - good balance of size and quality
    High       // Maximum compression, may take longer
}
```

### Color Schemes

```csharp
public enum PdfColorScheme
{
    Default,   // Blue header, standard colors
    Sober,     // Professional gray tones
    Vibrant,   // Bold, colorful theme
    Custom     // Define your own colors
}
```

## Usage Examples

### Basic PDF Generation

```csharp
// 1. Create template
var template = PdfTemplateBuilder.CreateOrderTemplate();

// 2. Get data
var orders = await GetOrdersAsync();

// 3. Export to PDF
await pdfExportService.ExportToPdfFileAsync(orders, template, "orders.pdf");
```

### Generate PDF to Memory

```csharp
var template = new PdfDocumentTemplate
{
    Title = "Product List",
    Columns = new[]
    {
        new PdfColumnConfig { PropertyName = "Name", DisplayName = "Product", Width = 2f },
        new PdfColumnConfig { PropertyName = "Price", DisplayName = "Price", Width = 1f }
    }
};

var products = await GetProductsAsync();
byte[] pdfBytes = await pdfExportService.ExportToPdfAsync(products, template);

// Return as file download
return File(pdfBytes, "application/pdf", "products.pdf");
```

### Optimize Existing PDF

```csharp
// Simple optimization (Balanced level)
await pdfService.OptimizeAsync("input.pdf", "output.pdf");

// With specific optimization level
var result = await pdfService.OptimizeAsync(
    source: "large-document.pdf",
    destination: "optimized-document.pdf",
    level: PdfOptimizationLevel.High
);

Console.WriteLine($"Reduced from {result.OriginalSize:N0} to {result.OptimizedSize:N0} bytes");
Console.WriteLine($"Compression: {result.CompressionRatio:P1}");
```

### Optimize PDF Stream

```csharp
using var inputStream = File.OpenRead("input.pdf");
using var outputStream = File.Create("output.pdf");

var result = await pdfService.OptimizeAsync(
    source: inputStream,
    destination: outputStream,
    level: PdfOptimizationLevel.Balanced
);

if (result.Success)
{
    Console.WriteLine($"Optimization successful: {result.CompressionRatio:P1} of original size");
}
```

### Generate and Optimize in One Step

```csharp
await pdfTextService.GenerateAndOptimizeAsync(
    filePath: "optimized-report.pdf",
    data: customers,
    template: customerTemplate,
    optimizationLevel: PdfOptimizationLevel.High
);
```

## PDF Templates

### Template Structure

```csharp
public class PdfDocumentTemplate
{
    public string Title { get; set; }                    // Document title
    public PageSize PageSize { get; set; }               // Page size (A4, Letter, etc.)
    public bool UseHeader { get; set; }                  // Include header
    public bool UseFooter { get; set; }                  // Include footer
    public string FooterTemplate { get; set; }           // Footer text template
    public PdfColorScheme ColorScheme { get; set; }      // Color scheme
    public PdfColumnConfig[] Columns { get; set; }       // Table columns
    public PdfSectionConfig[] Sections { get; set; }     // Document sections
}
```

### Column Configuration

```csharp
public class PdfColumnConfig
{
    public string PropertyName { get; set; }             // Property to display
    public string DisplayName { get; set; }              // Column header text
    public float Width { get; set; }                     // Relative width
    public Func<object, string> ValueFormatter { get; set; }  // Custom formatter
}
```

### Pre-built Templates

#### Order Template

```csharp
var orderTemplate = PdfTemplateBuilder.CreateOrderTemplate();

// Equivalent to:
var template = new PdfDocumentTemplate
{
    Title = "Orders",
    PageSize = PageSize.A4.Rotate(),  // Landscape
    UseFooter = true,
    FooterTemplate = "Orders Export",
    ColorScheme = PdfColorScheme.Sober,
    Columns = new[]
    {
        new PdfColumnConfig
        {
            PropertyName = "OrderNumber",
            DisplayName = "Order #",
            Width = 1f
        },
        new PdfColumnConfig
        {
            PropertyName = "CreationDate",
            DisplayName = "Created",
            Width = 1f,
            ValueFormatter = obj => ((DateTime)obj).ToShortDateString()
        },
        new PdfColumnConfig
        {
            PropertyName = "Customer.Code",
            DisplayName = "Customer",
            Width = 1f
        }
    }
};
```

#### Service Report Template

```csharp
var serviceTemplate = PdfTemplateBuilder.CreateServiceTemplate();

// Features sections instead of flat columns
```

### Custom Template Examples

#### Simple Product List

```csharp
var productTemplate = new PdfDocumentTemplate
{
    Title = "Product Catalog",
    PageSize = PageSize.A4,
    UseHeader = true,
    UseFooter = true,
    FooterTemplate = "Product Catalog - Generated {0}",  // {0} = page number
    ColorScheme = PdfColorScheme.Default,
    Columns = new[]
    {
        new PdfColumnConfig
        {
            PropertyName = "Sku",
            DisplayName = "SKU",
            Width = 1f
        },
        new PdfColumnConfig
        {
            PropertyName = "Name",
            DisplayName = "Product Name",
            Width = 3f
        },
        new PdfColumnConfig
        {
            PropertyName = "Price",
            DisplayName = "Price",
            Width = 1f,
            ValueFormatter = obj => $"${obj:F2}"
        },
        new PdfColumnConfig
        {
            PropertyName = "InStock",
            DisplayName = "Available",
            Width = 1f,
            ValueFormatter = obj => (bool)obj ? "Yes" : "No"
        }
    }
};
```

#### Customer Report with Nested Properties

```csharp
var customerTemplate = new PdfDocumentTemplate
{
    Title = "Customer Report",
    PageSize = PageSize.A4,
    ColorScheme = PdfColorScheme.Vibrant,
    Columns = new[]
    {
        new PdfColumnConfig
        {
            PropertyName = "CustomerNumber",
            DisplayName = "Customer #",
            Width = 1f
        },
        new PdfColumnConfig
        {
            PropertyName = "Name",
            DisplayName = "Name",
            Width = 2f
        },
        new PdfColumnConfig
        {
            PropertyName = "Address.City",  // Nested property
            DisplayName = "City",
            Width = 1.5f
        },
        new PdfColumnConfig
        {
            PropertyName = "TotalOrders",
            DisplayName = "Orders",
            Width = 1f
        },
        new PdfColumnConfig
        {
            PropertyName = "LastOrderDate",
            DisplayName = "Last Order",
            Width = 1.5f,
            ValueFormatter = obj =>
            {
                if (obj is DateTime date)
                    return date.ToString("MMM dd, yyyy");
                return "-";
            }
        }
    }
};
```

#### Multi-Section Report

```csharp
var reportTemplate = new PdfDocumentTemplate
{
    Title = "Comprehensive Report",
    UseHeader = true,
    UseFooter = true,
    ColorScheme = PdfColorScheme.Sober,
    Sections = new[]
    {
        new PdfSectionConfig
        {
            Title = "Summary Information",
            Columns = new[]
            {
                new PdfColumnConfig { PropertyName = "Date", DisplayName = "Date", Width = 1f },
                new PdfColumnConfig { PropertyName = "TotalAmount", DisplayName = "Total", Width = 1f }
            }
        },
        new PdfSectionConfig
        {
            Title = "Detailed Breakdown",
            Columns = new[]
            {
                new PdfColumnConfig { PropertyName = "ItemName", DisplayName = "Item", Width = 2f },
                new PdfColumnConfig { PropertyName = "Quantity", DisplayName = "Qty", Width = 1f },
                new PdfColumnConfig { PropertyName = "Amount", DisplayName = "Amount", Width = 1f }
            }
        }
    }
};
```

## Optimization

### How Optimization Works

The service uses a two-tier approach:

1. **Primary**: iLovePDF API (cloud-based, best compression)
2. **Fallback**: iText (local, reliable)

```csharp
var result = await pdfService.OptimizeAsync("input.pdf", "output.pdf");

// Automatically tries iLovePDF first, falls back to iText if needed
```

### Understanding Optimization Results

```csharp
public class PdfOptimizationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public long OriginalSize { get; set; }      // Bytes
    public long OptimizedSize { get; set; }     // Bytes
    public double CompressionRatio { get; set; } // 0.0 to 1.0 (percentage of original)
}

// Example usage
var result = await pdfService.OptimizeAsync("large.pdf", "small.pdf", PdfOptimizationLevel.High);

if (result.Success)
{
    var savedBytes = result.OriginalSize - result.OptimizedSize;
    var percentReduction = (1 - result.CompressionRatio) * 100;

    Console.WriteLine($"Original: {result.OriginalSize / 1024}KB");
    Console.WriteLine($"Optimized: {result.OptimizedSize / 1024}KB");
    Console.WriteLine($"Saved: {savedBytes / 1024}KB ({percentReduction:F1}% reduction)");
}
```

### Optimization Level Guidelines

```csharp
// Low - Fast processing, minimal compression (60-80% of original)
// Good for: Quick optimization, already optimized PDFs
await pdfService.OptimizeAsync("file.pdf", "out.pdf", PdfOptimizationLevel.Low);

// Balanced - Default, good compression (40-60% of original)
// Good for: General use, most documents
await pdfService.OptimizeAsync("file.pdf", "out.pdf", PdfOptimizationLevel.Balanced);

// High - Maximum compression (20-40% of original)
// Good for: Large files, archival, web delivery
await pdfService.OptimizeAsync("file.pdf", "out.pdf", PdfOptimizationLevel.High);
```

### Force Specific Optimizer

```csharp
// Use only iText (no iLovePDF)
var result = pdfOptimizationService.OptimizeWithIText(
    inputPath: "source.pdf",
    outputPath: "dest.pdf",
    level: PdfOptimizationLevel.High
);

// Use only iLovePDF (requires API key configured)
var result = await pdfOptimizationService.OptimizeWithILovePdfAsync(
    inputPath: "source.pdf",
    outputPath: "dest.pdf",
    level: PdfOptimizationLevel.High
);

// Check if iLovePDF is available
if (pdfOptimizationService.IsILovePdfAvailable)
{
    Console.WriteLine("iLovePDF API is configured and available");
}
```

## Dependency Injection Setup

### Basic Setup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register all PDF services
    services.AddScoped<IPdfExportService, PdfExportService>();
    services.AddScoped<IPdfTemplateService, PdfTemplateService>();
    services.AddScoped<IPdfOptimizationService, PdfOptimizationService>();
    services.AddScoped<IPdfService, PdfTextService>();
}
```

### With Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Configure iLovePDF if API key available
    services.Configure<PdfOptimizationConfig>(
        Configuration.GetSection("PdfOptimization")
    );

    services.AddScoped<IPdfExportService, PdfExportService>();
    services.AddScoped<IPdfTemplateService, PdfTemplateService>();
    services.AddScoped<IPdfOptimizationService, PdfOptimizationService>();
    services.AddScoped<IPdfService, PdfTextService>();
}
```

### appsettings.json

```json
{
  "PdfOptimization": {
    "ILovePdfApiKey": "your-api-key-here",
    "DefaultOptimizationLevel": "Balanced"
  }
}
```

## Common Scenarios

### Scenario 1: Export Database Query to PDF

```csharp
public class ReportService
{
    private readonly IPdfExportService _pdfExportService;
    private readonly IDbContext _dbContext;

    public async Task<byte[]> GenerateCustomerReportAsync()
    {
        // Query data
        var customers = await _dbContext.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Define template
        var template = new PdfDocumentTemplate
        {
            Title = "Active Customers",
            PageSize = PageSize.A4,
            UseFooter = true,
            FooterTemplate = "Customer Report",
            ColorScheme = PdfColorScheme.Default,
            Columns = new[]
            {
                new PdfColumnConfig { PropertyName = "CustomerNumber", DisplayName = "ID", Width = 1f },
                new PdfColumnConfig { PropertyName = "Name", DisplayName = "Name", Width = 2f },
                new PdfColumnConfig { PropertyName = "Email", DisplayName = "Email", Width = 2f },
                new PdfColumnConfig { PropertyName = "Phone", DisplayName = "Phone", Width = 1.5f }
            }
        };

        // Generate PDF
        return await _pdfExportService.ExportToPdfAsync(customers, template);
    }
}
```

### Scenario 2: Invoice Generation

```csharp
public class InvoiceService
{
    private readonly IPdfExportService _pdfExportService;

    public async Task<string> GenerateInvoicePdfAsync(Invoice invoice)
    {
        var template = new PdfDocumentTemplate
        {
            Title = $"Invoice {invoice.InvoiceNumber}",
            PageSize = PageSize.A4,
            UseHeader = true,
            UseFooter = true,
            FooterTemplate = $"Invoice {invoice.InvoiceNumber} - Page {{0}}",
            ColorScheme = PdfColorScheme.Sober,
            Columns = new[]
            {
                new PdfColumnConfig
                {
                    PropertyName = "Description",
                    DisplayName = "Description",
                    Width = 3f
                },
                new PdfColumnConfig
                {
                    PropertyName = "Quantity",
                    DisplayName = "Qty",
                    Width = 1f
                },
                new PdfColumnConfig
                {
                    PropertyName = "UnitPrice",
                    DisplayName = "Unit Price",
                    Width = 1f,
                    ValueFormatter = obj => $"${obj:F2}"
                },
                new PdfColumnConfig
                {
                    PropertyName = "Total",
                    DisplayName = "Total",
                    Width = 1f,
                    ValueFormatter = obj => $"${obj:F2}"
                }
            }
        };

        var filePath = $"Invoices/Invoice_{invoice.InvoiceNumber}.pdf";
        await _pdfExportService.ExportToPdfFileAsync(invoice.LineItems, template, filePath);

        return filePath;
    }
}
```

### Scenario 3: Batch PDF Optimization

```csharp
public class PdfBatchProcessor
{
    private readonly IPdfService _pdfService;

    public async Task OptimizeFolderAsync(string folderPath)
    {
        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
        var results = new List<(string File, PdfOptimizationResult Result)>();

        foreach (var file in pdfFiles)
        {
            var outputFile = Path.Combine(
                Path.GetDirectoryName(file),
                "optimized",
                Path.GetFileName(file)
            );

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            var result = await _pdfService.OptimizeAsync(
                source: file,
                destination: outputFile,
                level: PdfOptimizationLevel.High
            );

            results.Add((file, result));
        }

        // Report results
        var totalOriginal = results.Sum(r => r.Result.OriginalSize);
        var totalOptimized = results.Sum(r => r.Result.OptimizedSize);
        var totalSaved = totalOriginal - totalOptimized;

        Console.WriteLine($"Processed {results.Count} files");
        Console.WriteLine($"Total saved: {totalSaved / 1024 / 1024}MB");
    }
}
```

### Scenario 4: Generate PDF from API Response

```csharp
[HttpGet("export/orders")]
public async Task<IActionResult> ExportOrders([FromQuery] DateTime fromDate)
{
    var orders = await _orderService.GetOrdersAsync(fromDate);

    var template = PdfTemplateBuilder.CreateOrderTemplate();
    var pdfBytes = await _pdfExportService.ExportToPdfAsync(orders, template);

    return File(pdfBytes, "application/pdf", $"orders-{fromDate:yyyy-MM-dd}.pdf");
}
```

### Scenario 5: Optimized Report Download

```csharp
[HttpGet("reports/{reportId}/download")]
public async Task<IActionResult> DownloadReport(int reportId)
{
    var reportData = await _reportService.GetReportDataAsync(reportId);
    var template = CreateReportTemplate();

    // Generate to temp file
    var tempFile = Path.GetTempFileName() + ".pdf";
    var optimizedFile = Path.GetTempFileName() + ".pdf";

    try
    {
        // Generate PDF
        await _pdfExportService.ExportToPdfFileAsync(reportData, template, tempFile);

        // Optimize for web delivery
        var result = await _pdfService.OptimizeAsync(
            source: tempFile,
            destination: optimizedFile,
            level: PdfOptimizationLevel.High
        );

        // Return optimized version
        var fileBytes = await File.ReadAllBytesAsync(optimizedFile);
        return File(fileBytes, "application/pdf", $"report-{reportId}.pdf");
    }
    finally
    {
        // Cleanup
        try { File.Delete(tempFile); } catch { }
        try { File.Delete(optimizedFile); } catch { }
    }
}
```

### Scenario 6: Landscape vs Portrait

```csharp
// Wide data - use landscape
var wideTemplate = new PdfDocumentTemplate
{
    Title = "Wide Report",
    PageSize = PageSize.A4.Rotate(),  // Landscape
    Columns = new[] { /* many columns */ }
};

// Narrow data - use portrait
var narrowTemplate = new PdfDocumentTemplate
{
    Title = "Standard Report",
    PageSize = PageSize.A4,  // Portrait (default)
    Columns = new[] { /* fewer columns */ }
};
```

### Scenario 7: Custom Formatting

```csharp
var template = new PdfDocumentTemplate
{
    Title = "Sales Report",
    Columns = new[]
    {
        new PdfColumnConfig
        {
            PropertyName = "SaleDate",
            DisplayName = "Date",
            Width = 1f,
            ValueFormatter = obj =>
            {
                if (obj is DateTime date)
                    return date.ToString("MMM dd, yyyy");
                return "-";
            }
        },
        new PdfColumnConfig
        {
            PropertyName = "Amount",
            DisplayName = "Amount",
            Width = 1f,
            ValueFormatter = obj =>
            {
                if (obj is decimal amount)
                    return $"${amount:N2}";
                return "$0.00";
            }
        },
        new PdfColumnConfig
        {
            PropertyName = "Status",
            DisplayName = "Status",
            Width = 1f,
            ValueFormatter = obj =>
            {
                return obj?.ToString()?.ToUpper() ?? "PENDING";
            }
        }
    }
};
```

## Performance Considerations

### Memory Management

```csharp
// For large datasets, use file-based operations
await pdfExportService.ExportToPdfFileAsync(largeDataset, template, "output.pdf");

// Instead of loading into memory
byte[] pdfBytes = await pdfExportService.ExportToPdfAsync(largeDataset, template);
```

### Stream Operations

```csharp
// Use streams for better memory efficiency
using var outputStream = new MemoryStream();
await pdfService.OptimizeAsync(inputStream, outputStream);
```

### Parallel Processing

```csharp
// Process multiple PDFs in parallel
var files = Directory.GetFiles(folder, "*.pdf");
await Parallel.ForEachAsync(files, async (file, ct) =>
{
    var output = file.Replace(".pdf", "_optimized.pdf");
    await pdfService.OptimizeAsync(file, output, PdfOptimizationLevel.Balanced);
});
```

## Best Practices

1. **Choose appropriate optimization level** - Balanced is good for most cases
2. **Use landscape for wide tables** - Rotate page size for many columns
3. **Implement custom formatters** - Format dates, numbers, and special values properly
4. **Handle large datasets** - Use file-based operations for large exports
5. **Add page numbers** - Use footer templates with `{0}` placeholder
6. **Test color schemes** - Preview with different schemes for best appearance
7. **Clean up temp files** - Always delete temporary PDF files after use
8. **Async operations** - Use async methods for better scalability

## Related Documentation

- [Email Services](Email.md) - Email with PDF attachments
- [XML Services](XML.md) - XML serialization and data exchange
- [Azure Services](Azure.md) - Azure document translation for PDFs
