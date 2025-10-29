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
- Configurable application name for temp directory organization

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

## Core Features

### Services

#### Email Service with Templates
**Package**: CheapHelpers.Services

Full-featured email service with Fluid/Liquid template engine support.

**Features:**
- MailKit-based SMTP with attachment support
- Template engine with master templates and partials
- Development mode (auto-redirect to dev emails)
- Multiple recipients, CC/BCC support
- Embedded resource template loading

#### XmlService
**Package**: CheapHelpers.Services

XML serialization supporting both dynamic objects and strongly-typed models.

**Features:**
- Dynamic object serialization (ExpandoObject support)
- Strongly-typed serialization/deserialization
- File and string-based operations
- Async I/O operations

#### PDF & Document Services
**Package**: CheapHelpers.Services

- **PdfOptimizationService**: Dual optimization (iLovePDF API + iText fallback)
- **UblService**: UBL 2.0 (Universal Business Language) order document generation
- **PdfTemplateService**: Template-based PDF generation

### Security

#### EncryptionHelper
**Package**: CheapHelpers

Machine-specific AES-256-CBC encryption with PBKDF2 key derivation.

**Features:**
- Deterministic key generation based on machine/user/OS
- Fixed IV or random IV per operation
- Thread-safe lazy initialization
- Static `Encrypt()` / `Decrypt()` methods

#### FileHelper
**Package**: CheapHelpers

Secure filename generation to prevent overwrite attacks.

**Features:**
- `GetTrustedFileName()`: Generate secure filenames with GUID suffixes
- `GetTrustedFileNameFromPath()`: Secure naming from full paths
- `GetTrustedFileNameFromTempPath()`: Secure temp file handling

### Database

#### BaseRepo
**Package**: CheapHelpers.EF

Generic repository pattern with comprehensive CRUD operations.

**Features:**
- Full async CRUD operations with cancellation support
- Pagination support with `PaginatedList<T>`
- Bulk operations (AddRange, UpdateRange, DeleteRange)
- Query helpers (GetWhere, CountWhere, Exists)
- No-tracking queries for read operations
- Generic constraints (IEntityId, IEntityCode)

### Extension Methods

#### String Extensions
- `Capitalize()`: Capitalize first letter
- `IsDigitsOnly()`: Validate digit-only strings
- `ToInternationalPhoneNumber()`: Convert to international format (NL/BE support)
- `ToShortString()`: Truncate with ellipsis

#### DateTime Extensions
- `GetDateTime()`: Timezone conversion (bidirectional)
- `GetWorkingDays()`: Calculate business days with exclusions

#### Collection Extensions
- `Replace<T>()`: Replace items with predicates
- `ToBindingList()` / `ToObservableCollection()`: WPF/UI binding
- `IsNullOrEmpty()`: Safe null/empty check

#### Core Extensions
- `ToJson<T>()` / `FromJson<T>()`: JSON serialization with loop handling
- `DeepClone<T>()`: Deep object cloning via JSON
- `AddQueryParm()`: URI query string building

### Blazor Utilities

#### DownloadHelper
**Package**: CheapHelpers.Blazor

Client-side file downloads with multiple formats.

**Features:**
- HTML-to-image conversion (PNG/JPG via JS interop)
- Base64 image downloads
- File streaming downloads
- Auto-delete after download option

#### ClipboardService
**Package**: CheapHelpers.Blazor

Async clipboard operations via JS interop.

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

// Default: uses "CheapHelpers" as application name
using var tempManager = new TemporaryFileManager();

// Or specify custom application name for organization
// using var tempManager = new TemporaryFileManager(applicationName: "MyApp");

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

### XmlService

```csharp
using CheapHelpers.Services.DataExchange.Xml;

// Strongly-typed serialization
var myObject = new MyModel { Name = "Test", Value = 123 };
await xmlService.SerializeAsync("output.xml", myObject);

// Deserialization
var loaded = await xmlService.DeserializeAsync<MyModel>("output.xml");
```

### EncryptionHelper

```csharp
using CheapHelpers.Helpers.Encryption;

// Encrypt sensitive data (machine-specific key)
string encrypted = EncryptionHelper.Encrypt("sensitive data");

// Decrypt
string decrypted = EncryptionHelper.Decrypt(encrypted);

// Enhanced security with random IV per operation
string encryptedSecure = EncryptionHelper.EncryptWithRandomIV("top secret");
string decryptedSecure = EncryptionHelper.DecryptWithRandomIV(encryptedSecure);
```

### BaseRepo

```csharp
using CheapHelpers.EF.Repositories;

public class ProductRepo : BaseRepo<Product, MyDbContext>
{
    public ProductRepo(MyDbContext context) : base(context) { }
}

// Usage
var products = await productRepo.GetAllPaginatedAsync(pageIndex: 1, pageSize: 20);
var activeProducts = await productRepo.GetWhereAsync(p => p.IsActive);
await productRepo.AddAsync(newProduct);
```

### String Extensions

```csharp
using CheapHelpers.Extensions;

"hello world".Capitalize();  // "Hello world"
"0474123456".ToInternationalPhoneNumber("BE");  // "+32474123456"
"Very long text...".ToShortString(10);  // "Very lo..."
```

## Requirements

- .NET 10
- C# 14

## License

See [LICENSE.txt](LICENSE.txt)