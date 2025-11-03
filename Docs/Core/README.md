# CheapHelpers Core Documentation

Comprehensive documentation for the CheapHelpers core library - essential utilities, extension methods, and helpers for .NET development.

## Package Information

**Package:** CheapHelpers
**Version:** 1.1.3
**Target Framework:** .NET 10.0
**NuGet:** [CheapHelpers](https://www.nuget.org/packages/CheapHelpers)

## Overview

CheapHelpers Core provides fundamental utilities for everyday .NET development including:
- String manipulation and validation
- DateTime/DateTimeOffset operations and rounding
- Collection extensions and dynamic LINQ
- Memory caching with multiple expiration strategies
- AES-256 encryption with secure key generation
- Secure filename generation
- Process execution with progress tracking
- Bit manipulation and binary parsing
- Hashing utilities (MD5, FNV)

## Documentation Index

### String Operations
**[StringExtensions.md](StringExtensions.md)** - String manipulation and validation
- Capitalization and character filtering
- Phone number formatting (Dutch/Belgian)
- String truncation with ellipsis
- Special character removal and sanitization
- Validation helpers (digits only, boolean conversion)

### Date and Time
**[DateTimeExtensions.md](DateTimeExtensions.md)** - DateTime and DateTimeOffset utilities
- Timezone conversions
- Working day calculations
- Temporal rounding (Floor, Round, Ceiling)
- Time-series bucketing (PerMinute, PerHour, PerDay)
- Business calendar support

### Collections
**[CollectionExtensions.md](CollectionExtensions.md)** - Collection manipulation and conversion
- Item replacement in lists
- Type conversions (BindingList, ObservableCollection)
- Null/empty checking
- Dynamic LINQ ordering by property name
- Runtime-based sorting

### Caching
**[Caching.md](Caching.md)** - Thread-safe memory caching implementations
- **AbsoluteExpirationCache**: Fixed-time expiration
- **SlidingExpirationCache**: Inactivity-based expiration
- **FlexibleExpirationCache**: Combined expiration strategies
- GetOrAdd patterns, cache warming, monitoring

### Encryption
**[Encryption.md](Encryption.md)** - AES-256 encryption utilities
- Machine-specific key generation
- Random IV encryption (RECOMMENDED for sensitive data)
- Static IV encryption (for URL parameters, cache keys)
- Security best practices and compliance guidance

### File Operations
**[FileHelpers.md](FileHelpers.md)** - Secure filename generation
- GUID-suffixed filenames (prevent overwrites)
- Date-based naming (daily, weekly, monthly, yearly)
- Custom date format support
- Temporary file management

### Process Execution
**[ProcessExecution.md](ProcessExecution.md)** - External process wrapper
- Progress tracking with regex patterns
- Timeout and cancellation support
- Process piping (stdout to stdin)
- Environment variable configuration
- Built-in patterns for FFmpeg, VapourSynth, and common tools

### Binary Operations
**[BitHelper.md](BitHelper.md)** - Bit manipulation and byte conversion
- Bit extraction from bytes/shorts
- Byte concatenation
- Little-endian parsing (Int16, Int32, UInt16, UInt32)
- Hexadecimal string conversions
- Binary file parsing utilities

### Hashing
**[HashHelper.md](HashHelper.md)** - Hashing and distribution utilities
- MD5 hashing (checksums, content deduplication)
- FNV hash (load balancing, data partitioning)
- Hexadecimal conversions
- ETag generation

## Quick Start

### Installation

```bash
dotnet add package CheapHelpers
```

### Basic Usage Examples

**String Sanitization:**
```csharp
using CheapHelpers.Extensions;

string userInput = "My File (2024).pdf";
string safeName = userInput.Sanitize();
// Result: "My_File_2024.pdf"
```

**DateTime Rounding:**
```csharp
using CheapHelpers.Extensions;

var timestamp = DateTimeOffset.UtcNow;
var hourBucket = timestamp.PerHour();
// Groups timestamps by hour for analytics
```

**Memory Caching:**
```csharp
using CheapHelpers.Caching;

var cache = new SlidingExpirationCache<User>("UserCache", TimeSpan.FromMinutes(30));

var user = await cache.GetOrAddAsync("user_123", async key =>
{
    return await LoadUserFromDatabase(123);
});
```

**Encryption:**
```csharp
using CheapHelpers.Helpers.Encryption;

// Encrypt sensitive data
string encrypted = EncryptionHelper.EncryptWithRandomIV("MyPassword123");

// Decrypt
string decrypted = EncryptionHelper.DecryptWithRandomIV(encrypted);
```

**Secure Filenames:**
```csharp
using CheapHelpers.Helpers.Files;

string secure = FileHelper.GetDailyFilename(DateTime.Today, "backup.sql");
// Result: "backup_2024-01-15_a1b2c3d4.sql"
```

**Process Execution:**
```csharp
using CheapHelpers.Process;

var result = await ProcessExecutor.ExecuteAsync(
    "ffmpeg",
    "-i input.mp4 output.mp4",
    new ProcessExecutorOptions { Timeout = TimeSpan.FromMinutes(10) });

if (result.Success)
    Console.WriteLine("Conversion completed!");
```

## Dependencies

- **Microsoft.Extensions.Caching.Memory** (9.0.10) - Memory caching infrastructure
- **MimeMapping** (3.1.0) - MIME type detection
- **MoreLinq** (4.4.0) - Additional LINQ methods
- **Newtonsoft.Json** (13.0.4) - JSON serialization
- **System.Security.Cryptography.ProtectedData** (9.0.10) - Cryptography support

## Architecture

### Extension Methods Pattern
All extension methods follow consistent patterns:
- Null/empty validation with clear error messages
- Fluent, chainable APIs
- Performance-optimized implementations

### Caching Strategy
Three-tier caching approach:
1. **Absolute**: For time-sensitive data with fixed expiration
2. **Sliding**: For user-specific data that should persist while active
3. **Flexible**: For hybrid scenarios requiring both strategies

### Security Model
- **Encryption**: Machine-specific deterministic key generation
- **File Security**: GUID suffixes prevent file overwrite attacks
- **Process Execution**: Safe command execution with timeout protection

## Best Practices

### String Operations
- Use `Sanitize()` for user-generated filenames
- Prefer `TrimWithEllipsis()` over manual substring for display truncation
- Validate input with `IsDigitsOnly()` before parsing

### Caching
- Default to `SlidingExpirationCache` for most scenarios
- Use `GetOrAdd` pattern to avoid race conditions
- Implement cache warming for critical data
- Monitor cache hits/misses for optimization

### Encryption
- **Always** use `EncryptWithRandomIV` for sensitive data
- Only use static IV methods for deterministic requirements (URL params, cache keys)
- Implement proper key rotation strategies for production
- Consider Azure Key Vault or HSM for high-security scenarios

### Process Execution
- Always set timeouts to prevent hanging processes
- Use cancellation tokens for user-cancellable operations
- Test progress patterns with actual tool output
- Handle different exit codes appropriately

### File Operations
- Use date-based naming for temporal organization
- Combine GUID filenames with directory structures
- Store secure filenames in database with original names
- Implement cleanup based on creation dates

## Common Patterns

### Cache-Aside Pattern
```csharp
var cache = new SlidingExpirationCache<Product>("Products", TimeSpan.FromMinutes(15));

public async Task<Product> GetProduct(int id)
{
    return await cache.GetOrAddAsync($"product_{id}", async key =>
    {
        return await database.LoadProductAsync(id);
    });
}
```

### Secure File Upload
```csharp
public async Task<string> SaveUpload(IFormFile file)
{
    string secureName = FileHelper.GetTrustedFileName(file.FileName);
    string path = Path.Combine(uploadPath, secureName);
    await file.CopyToAsync(new FileStream(path, FileMode.Create));
    return secureName;
}
```

### Time-Series Bucketing
```csharp
var buckets = measurements
    .GroupBy(m => m.Timestamp.PerHour())
    .Select(g => new { Hour = g.Key, Average = g.Average(x => x.Value) });
```

### Dynamic Grid Sorting
```csharp
public IQueryable<T> ApplySort<T>(IQueryable<T> query, string column, bool ascending)
{
    return ascending
        ? query.OrderByDynamic(column)
        : query.OrderByDescendingDynamic(column);
}
```

## Performance Considerations

- **Caching**: All cache implementations are thread-safe with minimal locking
- **String Extensions**: Use StringBuilder internally for optimal string building
- **DateTime Rounding**: Tick-based arithmetic for maximum performance
- **Process Execution**: Async/await throughout prevents thread blocking
- **Bit Operations**: Bitwise operations for efficient bit manipulation

## Version History

**1.1.3** (Current)
- Updated to .NET 10.0
- Enhanced documentation
- Improved null safety

## Support and Contributing

- **Repository**: [github.com/CheapNud/CheapHelpers](https://github.com/CheapNud/CheapHelpers)
- **Issues**: Report bugs and feature requests on GitHub
- **License**: MIT

## Related Documentation

- **[CheapHelpers.Blazor](../Blazor/)** - Blazor-specific helpers and components
- **[CheapHelpers.EF](../EF/)** - Entity Framework utilities
- **[CheapHelpers.MAUI](../MAUI/)** - MAUI/Xamarin helpers
- **[CheapHelpers.Networking](../Networking/)** - Network utilities
- **[CheapHelpers.Services](../Services/)** - Service layer helpers

## See Also

Each documentation file includes:
- Detailed method signatures
- Comprehensive examples
- Common use cases
- Tips and best practices
- Security considerations (where applicable)

Start with the topic most relevant to your needs, or explore the full documentation for comprehensive coverage of all utilities.
