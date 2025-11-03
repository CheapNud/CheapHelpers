# HashHelper

Hashing utilities with MD5, FNV, and hexadecimal conversion support.

## Overview

The `HashHelper` class provides hashing functions and hexadecimal string conversions. It includes MD5 hashing, FNV (Fowler-Noll-Vo) hash for distribution purposes, and utility methods for working with byte arrays and hex strings.

## Namespace

```csharp
using CheapHelpers.Helpers.Encryption;
```

## MD5 Hashing

### CalculateMD5Hash (byte array)

Calculates MD5 hash from a byte array.

**Signature:**
```csharp
public static string CalculateMD5Hash(byte[] input, string format = "X2")
```

**Parameters:**
- `input`: Byte array to hash
- `format`: Format string for byte conversion (default: "X2" for uppercase hex)

**Returns:** MD5 hash as hexadecimal string

**Example:**
```csharp
byte[] data = { 0x48, 0x65, 0x6C, 0x6C, 0x6F };  // "Hello"
string hash = HashHelper.CalculateMD5Hash(data);
// Result: "8B1A9953C4611296A827ABF8C47804D7" (uppercase)

string lowerHash = HashHelper.CalculateMD5Hash(data, "x2");
// Result: "8b1a9953c4611296a827abf8c47804d7" (lowercase)
```

### CalculateMD5Hash (string)

Calculates MD5 hash from a string.

**Signature:**
```csharp
public static string CalculateMD5Hash(string input, string format = "X2")
```

**Parameters:**
- `input`: String to hash
- `format`: Format string for byte conversion (default: "X2" for uppercase hex)

**Returns:** MD5 hash as hexadecimal string

**Example:**
```csharp
string text = "Hello World";
string hash = HashHelper.CalculateMD5Hash(text);
// Result: "B10A8DB164E0754105B7A99BE72E3FE5"

string lowerHash = HashHelper.CalculateMD5Hash(text, "x2");
// Result: "b10a8db164e0754105b7a99be72e3fe5"
```

## FNV Hashing

### FNVHash

Calculates FNV (Fowler-Noll-Vo) hash for a gateway or path string. If a full path with pipe delimiter is provided, only the gateway portion (before first pipe) is hashed. Returns a hash value between 0-99 for distribution purposes.

**Signature:**
```csharp
public static long FNVHash(string gatewayOrPath)
```

**Parameters:**
- `gatewayOrPath`: Gateway name or full path with pipe delimiters

**Returns:** FNV hash value (0-99), or -1 on error

**Example:**
```csharp
// Gateway only
long hash1 = HashHelper.FNVHash("server1");
// Result: 0-99

// Full path - only gateway portion is hashed
long hash2 = HashHelper.FNVHash("server1|database|table");
// Same as: HashHelper.FNVHash("server1")

// Use for distribution
string server = GetServerByHash(HashHelper.FNVHash(gatewayName));
```

### FNVHashFull

Calculates full FNV (Fowler-Noll-Vo) hash implementation. Returns a hash value between 0-99 for distribution purposes.

**Signature:**
```csharp
public static long FNVHashFull(string input)
```

**Parameters:**
- `input`: String to hash

**Returns:** FNV hash value (0-99)

**Example:**
```csharp
long hash = HashHelper.FNVHashFull("user12345");
// Result: 0-99

// Distribute users across 100 buckets
int bucket = (int)HashHelper.FNVHashFull(userId);
string bucketPath = $"bucket_{bucket:D2}";
```

## Hexadecimal Conversions

### ByteArrayToHexString

Converts a byte array to a hexadecimal string (lowercase).

**Signature:**
```csharp
public static string ByteArrayToHexString(byte[] bytes)
```

**Parameters:**
- `bytes`: Byte array to convert

**Returns:** Hexadecimal string representation (lowercase, without 0x prefix)

**Example:**
```csharp
byte[] bytes = { 0xDE, 0xAD, 0xBE, 0xEF };
string hex = HashHelper.ByteArrayToHexString(bytes);
// Result: "deadbeef"
```

### HexStringToByteArray

Converts a hexadecimal string to a byte array.

**Signature:**
```csharp
public static byte[] HexStringToByteArray(string hexString)
```

**Parameters:**
- `hexString`: Hexadecimal string (without 0x prefix)

**Returns:** Byte array representation of the hex string

**Example:**
```csharp
byte[] bytes = HashHelper.HexStringToByteArray("DEADBEEF");
// Result: [0xDE, 0xAD, 0xBE, 0xEF]

byte[] hash = HashHelper.HexStringToByteArray("a1b2c3d4");
// Result: [0xA1, 0xB2, 0xC3, 0xD4]
```

## Common Use Cases

### File Integrity Verification

```csharp
public class FileIntegrityChecker
{
    public string CalculateFileHash(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        return HashHelper.CalculateMD5Hash(fileData);
    }

    public bool VerifyFileIntegrity(string filePath, string expectedHash)
    {
        string actualHash = CalculateFileHash(filePath);
        return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<string, string>> GenerateManifest(string[] files)
    {
        var manifest = new Dictionary<string, string>();

        foreach (var file in files)
        {
            string hash = CalculateFileHash(file);
            manifest[Path.GetFileName(file)] = hash;
        }

        return manifest;
    }
}
```

### Content Deduplication

```csharp
public class ContentDeduplication
{
    private readonly Dictionary<string, string> _contentHashes = new();

    public string StoreContent(string content)
    {
        string hash = HashHelper.CalculateMD5Hash(content);

        if (!_contentHashes.ContainsKey(hash))
        {
            // Store new content
            string storagePath = $"storage/{hash}.txt";
            File.WriteAllText(storagePath, content);
            _contentHashes[hash] = storagePath;
        }

        return hash;
    }

    public string RetrieveContent(string hash)
    {
        if (_contentHashes.TryGetValue(hash, out string path))
        {
            return File.ReadAllText(path);
        }

        return null;
    }
}
```

### Load Balancing with FNV Hash

```csharp
public class LoadBalancer
{
    private readonly List<string> _servers;

    public LoadBalancer(List<string> servers)
    {
        _servers = servers;
    }

    public string GetServerForUser(string userId)
    {
        // Distribute users across servers using FNV hash
        long hash = HashHelper.FNVHashFull(userId);
        int serverIndex = (int)(hash % _servers.Count);
        return _servers[serverIndex];
    }

    public string GetServerForGateway(string gatewayPath)
    {
        // Extract gateway portion and distribute
        long hash = HashHelper.FNVHash(gatewayPath);
        int serverIndex = (int)(hash % _servers.Count);
        return _servers[serverIndex];
    }
}
```

### Caching with Content Hash Keys

```csharp
public class ContentCache
{
    private readonly AbsoluteExpirationCache<string> _cache;

    public ContentCache()
    {
        _cache = new AbsoluteExpirationCache<string>(
            "ContentCache",
            TimeSpan.FromHours(1));
    }

    public string GetOrComputeContent(string input)
    {
        // Use MD5 hash as cache key
        string cacheKey = HashHelper.CalculateMD5Hash(input);

        return _cache.GetOrAdd(cacheKey, key =>
        {
            // Expensive computation
            return ProcessContent(input);
        });
    }

    private string ProcessContent(string input)
    {
        // Simulate expensive operation
        Thread.Sleep(1000);
        return input.ToUpper();
    }
}
```

### Data Partitioning

```csharp
public class DataPartitioner
{
    private const int PartitionCount = 100;

    public string GetPartitionPath(string recordId)
    {
        // Use FNV hash to distribute records across partitions
        long hash = HashHelper.FNVHashFull(recordId);
        int partition = (int)hash;  // Already 0-99

        return $@"data\partition_{partition:D2}\records.db";
    }

    public void StoreRecord(string recordId, object data)
    {
        string partitionPath = GetPartitionPath(recordId);

        // Ensure partition directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(partitionPath));

        // Store in appropriate partition
        AppendToPartition(partitionPath, recordId, data);
    }

    public object LoadRecord(string recordId)
    {
        string partitionPath = GetPartitionPath(recordId);
        return ReadFromPartition(partitionPath, recordId);
    }
}
```

### Download Verification

```csharp
public class DownloadManager
{
    public async Task<bool> DownloadAndVerify(
        string url,
        string targetPath,
        string expectedMd5)
    {
        // Download file
        using var client = new HttpClient();
        byte[] data = await client.GetByteArrayAsync(url);

        // Verify hash before saving
        string actualMd5 = HashHelper.CalculateMD5Hash(data);

        if (!actualMd5.Equals(expectedMd5, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Hash mismatch. Expected: {expectedMd5}, Actual: {actualMd5}");
        }

        // Hash verified - save file
        await File.WriteAllBytesAsync(targetPath, data);
        return true;
    }
}
```

### Password Hashing (Legacy - Not Recommended)

```csharp
public class LegacyPasswordService
{
    // WARNING: MD5 is NOT secure for password hashing
    // Use bcrypt, Argon2, or PBKDF2 instead
    // This is for legacy system compatibility only

    [Obsolete("MD5 is not secure for passwords. Use modern password hashing.")]
    public string HashPassword(string password)
    {
        // Add salt to improve security slightly
        string salted = $"{password}|{Environment.MachineName}";
        return HashHelper.CalculateMD5Hash(salted);
    }

    [Obsolete("MD5 is not secure for passwords. Use modern password hashing.")]
    public bool VerifyPassword(string password, string storedHash)
    {
        string hash = HashPassword(password);
        return hash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
```

### ETag Generation

```csharp
public class ETagGenerator
{
    public string GenerateETag(string content)
    {
        // Generate ETag from content hash
        string hash = HashHelper.CalculateMD5Hash(content);
        return $"\"{hash}\"";
    }

    public string GenerateETag(byte[] data)
    {
        string hash = HashHelper.CalculateMD5Hash(data);
        return $"\"{hash}\"";
    }

    public bool ValidateETag(string content, string etag)
    {
        string expectedETag = GenerateETag(content);
        return expectedETag.Equals(etag, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Consistent Hashing Ring

```csharp
public class ConsistentHashRing
{
    private readonly SortedDictionary<long, string> _ring = new();
    private const int VirtualNodes = 100;

    public void AddNode(string nodeName)
    {
        for (int i = 0; i < VirtualNodes; i++)
        {
            string virtualNode = $"{nodeName}:{i}";
            long hash = HashHelper.FNVHashFull(virtualNode);
            _ring[hash] = nodeName;
        }
    }

    public string GetNode(string key)
    {
        long hash = HashHelper.FNVHashFull(key);

        // Find first node >= hash
        foreach (var kvp in _ring)
        {
            if (kvp.Key >= hash)
                return kvp.Value;
        }

        // Wrap around to first node
        return _ring.First().Value;
    }
}
```

## Tips and Best Practices

1. **MD5 Security**: MD5 is **NOT cryptographically secure** for sensitive data. Use it only for:
   - File integrity checks (checksums)
   - Content deduplication
   - ETags
   - Non-security cache keys

   For passwords and sensitive data, use:
   - bcrypt
   - Argon2
   - PBKDF2
   - SHA-256/SHA-512 with proper salting

2. **FNV Hash Distribution**: FNV hash returns 0-99, making it ideal for:
   - Distributing data across 100 buckets
   - Load balancing across up to 100 servers
   - Data partitioning

   For more buckets, modify the modulo operation in `FNVHashFull`.

3. **Hash Format**: MD5 hashes can be uppercase or lowercase. Always use case-insensitive comparison:
   ```csharp
   bool isMatch = hash1.Equals(hash2, StringComparison.OrdinalIgnoreCase);
   ```

4. **Performance**: MD5 is fast but not as fast as FNV. For high-throughput scenarios needing distribution (not security), prefer FNV.

5. **Hex String Format**: Hex strings should not include "0x" prefix:
   ```csharp
   // Correct
   byte[] bytes = HashHelper.HexStringToByteArray("DEADBEEF");

   // Incorrect - will fail
   byte[] bytes = HashHelper.HexStringToByteArray("0xDEADBEEF");
   ```

6. **Gateway Extraction**: `FNVHash` automatically extracts the gateway portion before the first pipe. This is useful for hierarchical paths:
   ```csharp
   // Both produce same hash
   long hash1 = HashHelper.FNVHash("server1");
   long hash2 = HashHelper.FNVHash("server1|db|table");
   ```

7. **Collision Handling**: MD5 has a small collision probability. For critical applications, implement collision handling:
   ```csharp
   string hash = HashHelper.CalculateMD5Hash(content);
   if (_storage.ContainsKey(hash))
   {
       // Collision detected - verify content matches
       if (!_storage[hash].Equals(content))
           throw new InvalidOperationException("Hash collision detected");
   }
   ```

8. **Large Files**: For large files, read in chunks to avoid memory issues:
   ```csharp
   using var md5 = MD5.Create();
   using var stream = File.OpenRead(filePath);
   byte[] hash = md5.ComputeHash(stream);
   string hexHash = HashHelper.ByteArrayToHexString(hash);
   ```

9. **Case Sensitivity**: Hash outputs are case-sensitive strings. Store them consistently (all upper or all lower) in databases:
   ```csharp
   string hash = HashHelper.CalculateMD5Hash(data).ToLowerInvariant();
   ```

10. **FNV Alternatives**: For more sophisticated distribution needs, consider:
    - MurmurHash3
    - xxHash
    - CityHash
    - SHA-256 (slower but cryptographically secure)

11. **Cache Warming**: Pre-calculate hashes for frequently accessed content:
    ```csharp
    var contentHashes = contents
        .AsParallel()
        .Select(c => new { Content = c, Hash = HashHelper.CalculateMD5Hash(c) })
        .ToDictionary(x => x.Hash, x => x.Content);
    ```

12. **Monitoring**: Log hash collisions and distribution for FNV:
    ```csharp
    var distribution = keys
        .Select(k => HashHelper.FNVHashFull(k))
        .GroupBy(h => h)
        .Select(g => new { Hash = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count);

    // Analyze for hotspots
    ```
