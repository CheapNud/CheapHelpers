# CheapHelpers

A collection of C# utility classes and extension methods for common development tasks.

## Latest Additions

### Caching

#### Memory Cache System
Modern, thread-safe memory caching with flexible expiration strategies using `Microsoft.Extensions.Caching.Memory`.

**Features:**
- Three expiration strategies: Absolute, Sliding, and Flexible (both)
- Full async/await support with cancellation
- Generic type-safe implementation
- Factory pattern for cache-miss scenarios
- Proper disposal pattern (IDisposable)
- TryGet pattern for optional retrieval
- Thread-safe operations

**Cache Types:**
- **AbsoluteExpirationCache\<T>**: Items expire at a fixed time after creation
- **SlidingExpirationCache\<T>**: Items expire after inactivity (timer resets on access)
- **FlexibleExpirationCache\<T>**: Supports both absolute and sliding expiration simultaneously

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

## Packages

| Package | Version | Description |
|---------|---------|-------------|
| [CheapHelpers](https://www.nuget.org/packages/CheapHelpers) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.svg) | Core utilities, extensions, and helpers |
| [CheapHelpers.Models](https://www.nuget.org/packages/CheapHelpers.Models) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Models.svg) | Shared data models and DTOs |
| [CheapHelpers.EF](https://www.nuget.org/packages/CheapHelpers.EF) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.EF.svg) | Entity Framework repository pattern |
| [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Services.svg) | Business services and integrations |
| [CheapHelpers.Blazor](https://www.nuget.org/packages/CheapHelpers.Blazor) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Blazor.svg) | Blazor components and UI utilities |

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

### Via NuGet Package Manager

```bash
# Core utilities and extensions
dotnet add package CheapHelpers

# Entity Framework repository pattern
dotnet add package CheapHelpers.EF

# Business services (email, PDF, Azure)
dotnet add package CheapHelpers.Services

# Blazor components and UI utilities
dotnet add package CheapHelpers.Blazor

# Shared models and DTOs
dotnet add package CheapHelpers.Models
```

### Via Package Manager Console (Visual Studio)

```powershell
Install-Package CheapHelpers
Install-Package CheapHelpers.EF
Install-Package CheapHelpers.Services
Install-Package CheapHelpers.Blazor
Install-Package CheapHelpers.Models
```

### Via Project Reference (for development)

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

### Memory Caching

```csharp
using CheapHelpers.Caching;

// Absolute expiration - items expire exactly 1 hour after creation
using var userCache = new AbsoluteExpirationCache<User>("UserCache", TimeSpan.FromHours(1));

// Fetch user or get from cache
var user = await userCache.GetOrAddAsync("user:123", async key =>
    await database.GetUserAsync(key));

// Sliding expiration - items expire after 30 minutes of inactivity
using var sessionCache = new SlidingExpirationCache<Session>("SessionCache", TimeSpan.FromMinutes(30));
var session = sessionCache.GetOrAdd("session:abc", key => CreateNewSession(key));

// Flexible - max 24 hours absolute, but extends on access (1 hour sliding)
using var apiCache = new FlexibleExpirationCache<ApiResponse>(
    cacheName: "ApiCache",
    absoluteExpiration: TimeSpan.FromHours(24),
    slidingExpiration: TimeSpan.FromHours(1));

var apiData = await apiCache.GetOrAddAsync("endpoint:/users", async key =>
{
    var response = await httpClient.GetAsync(key);
    return await response.Content.ReadFromJsonAsync<ApiResponse>();
});

// Manual operations
apiCache.Set("key", value);
if (apiCache.TryGet("key", out var cachedValue))
{
    Debug.WriteLine($"Found: {cachedValue}");
}
apiCache.Remove("key");
apiCache.Clear(); // Remove all items
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

- .NET 10.0 or later
- C# 14.0 or later

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Building from Source

```bash
git clone https://github.com/CheapNud/CheapHelpers.git
cd CheapHelpers
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Creating a Release

Releases are automatically published to NuGet.org when a version tag is pushed:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.