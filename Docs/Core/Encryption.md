# EncryptionHelper

AES-256 obfuscation with a key derived from the machine name and the user name.

## Overview

The `EncryptionHelper` class provides AES-256-CBC encryption and decryption methods with two approaches:
- **Deterministic** (`EncryptDeterministic`/`DecryptDeterministic`) - for URL parameters and cache keys
- **Random IV** (`EncryptWithRandomIV`/`DecryptWithRandomIV`) - for sensitive data (RECOMMENDED)

## Namespace

```csharp
using CheapHelpers.Helpers.Encryption;
```

## Security Model

The encryption key is **machine-specific** and **deterministic**, generated from:
- Machine name
- User name

The key is the same for every build of the library on that machine for that user, and anyone who knows both values can rebuild it. It is not for secrets: use ASP.NET Core Data Protection or `PassphraseEnvelope` for those.

## Methods

### EncryptWithRandomIV (RECOMMENDED)

Encrypts data with a cryptographically secure random IV for each operation.

**Signature:**
```csharp
public static string EncryptWithRandomIV(string plainText)
```

**Parameters:**
- `plainText`: Text to encrypt

**Returns:** Base64 encoded string containing IV + encrypted data

**Security Benefits:**
- Each encryption produces different ciphertext for identical plaintext
- Prevents pattern analysis attacks
- Follows NIST cryptographic best practices
- Suitable for encrypting sensitive data

**Example:**
```csharp
string password = "MySecretPassword123";
string encrypted = EncryptionHelper.EncryptWithRandomIV(password);

// Same input produces different output each time
string encrypted2 = EncryptionHelper.EncryptWithRandomIV(password);
// encrypted != encrypted2

// Store in database
userRecord.EncryptedPassword = encrypted;
```

### DecryptWithRandomIV

Decrypts data that was encrypted with `EncryptWithRandomIV`.

**Signature:**
```csharp
public static string DecryptWithRandomIV(string cipherText)
```

**Parameters:**
- `cipherText`: Base64 encoded string containing IV + encrypted data

**Returns:** Decrypted plain text string

**Throws:** `InvalidOperationException` if decryption fails

**Example:**
```csharp
string encrypted = userRecord.EncryptedPassword;
string password = EncryptionHelper.DecryptWithRandomIV(encrypted);
```

### EncryptDeterministic

Encrypts data using a static, machine-specific IV. Same plaintext always produces the same ciphertext.

**Signature:**
```csharp
public static string EncryptDeterministic(string plainText)
```

**Parameters:**
- `plainText`: Text to encrypt

**Returns:** Base64 encoded encrypted string

**Security Note:** Uses static IV — only use when deterministic output is required.

**Valid Use Cases:**
- URL/route parameters that need to be matched or compared
- Cache keys or database lookups where deterministic encryption is required
- Scenarios needing same plaintext = same ciphertext for deduplication

**Example:**
```csharp
// URL parameter encryption (deterministic required)
string userId = "12345";
string encryptedId = EncryptionHelper.EncryptDeterministic(userId);
string url = $"https://example.com/user/{encryptedId}";

// Same user ID always produces same URL
string encryptedId2 = EncryptionHelper.EncryptDeterministic(userId);
// encryptedId == encryptedId2 (deterministic)
```

### DecryptDeterministic

Decrypts data that was encrypted with `EncryptDeterministic`.

**Signature:**
```csharp
public static string DecryptDeterministic(string cipherText)
```

**Parameters:**
- `cipherText`: Base64 encoded encrypted text

**Returns:** Decrypted plain text string

**Throws:** `InvalidOperationException` if decryption fails

**Example:**
```csharp
string encryptedId = GetUrlParameter("id");
string userId = EncryptionHelper.DecryptDeterministic(encryptedId);
```

## Algorithm Details

**Encryption Standard:** AES-256-CBC
- **Key Size:** 256 bits (32 bytes)
- **IV Size:** 128 bits (16 bytes)
- **Mode:** Cipher Block Chaining (CBC)
- **Padding:** PKCS7

**Key Derivation:** PBKDF2-HMAC-SHA256
- **Iterations:** 10,000
- **Salt:** SHA256 hash of machine name and user name

## Compatibility

Ciphertext from versions that mixed in the OS version and the assembly name does not decrypt with this version. Decrypt it before upgrading, or treat it as lost.

## Common Use Cases

### Encrypted URL Parameters (Deterministic)

```csharp
public class UrlGenerator
{
    public string GenerateSecureUrl(int recordId)
    {
        // Use static IV for consistent URLs
        string encryptedId = EncryptionHelper.EncryptDeterministic(recordId.ToString());

        return $"https://example.com/record/{encryptedId}";
    }

    public int DecodeRecordId(string encryptedId)
    {
        string decrypted = EncryptionHelper.DecryptDeterministic(encryptedId);

        return int.Parse(decrypted);
    }
}
```

### Cache Key Encryption

```csharp
public class SecureCacheService
{
    private readonly AbsoluteExpirationCache<string> _cache;

    public SecureCacheService()
    {
        _cache = new AbsoluteExpirationCache<string>(
            "SecureCache",
            TimeSpan.FromHours(1));
    }

    public void StoreSecureData(string key, string sensitiveData)
    {
        // Encrypt the data with random IV (RECOMMENDED)
        string encrypted = EncryptionHelper.EncryptWithRandomIV(sensitiveData);

        // Use deterministic encryption for cache key if needed
        string cacheKey = EncryptionHelper.EncryptDeterministic(key);

        _cache.Set(cacheKey, encrypted);
    }

    public string RetrieveSecureData(string key)
    {
        string cacheKey = EncryptionHelper.EncryptDeterministic(key);

        var encrypted = _cache.GetCachedItem(cacheKey);
        if (encrypted == null)
            return null;

        return EncryptionHelper.DecryptWithRandomIV(encrypted);
    }
}
```

## Migration from Static to Random IV

If you have existing data encrypted with the static IV method and want to migrate to random IV:

```csharp
public class EncryptionMigration
{
    public async Task MigrateUserData()
    {
        var users = await database.GetAllUsersAsync();

        foreach (var user in users)
        {
            if (IsStaticIvEncryption(user.EncryptedApiKey))
            {
                // Decrypt with deterministic method
                string plainText = EncryptionHelper.DecryptDeterministic(user.EncryptedApiKey);

                // Re-encrypt with new method
                user.EncryptedApiKey = EncryptionHelper.EncryptWithRandomIV(plainText);

                await database.UpdateAsync(user);
            }
        }
    }

    private bool IsStaticIvEncryption(string encrypted)
    {
        // Add logic to detect encryption type
        // Option 1: Add a prefix/version marker
        // Option 2: Track migration status separately
        // Option 3: Try both methods and see which succeeds
        return !encrypted.StartsWith("v2:");
    }
}
```

## Tips and Best Practices

1. **Default to Random IV**: Always use `EncryptWithRandomIV` for encrypting sensitive data unless you have a specific requirement for deterministic encryption.

2. **Key Rotation**: Since the key is machine-specific and deterministic, rotating keys requires:
   - Decrypting all data with the old key
   - Changing system characteristics (if possible) or implementing custom key storage
   - Re-encrypting with the new key

3. **Error Handling**: Always wrap decrypt operations in try-catch:
   ```csharp
   try
   {
       string decrypted = EncryptionHelper.DecryptWithRandomIV(cipherText);
   }
   catch (InvalidOperationException ex)
   {
       logger.LogError(ex, "Decryption failed - data may be corrupted");
       // Handle corrupted data
   }
   ```

4. **Empty Strings**: Both methods return empty string for null or empty input without throwing exceptions.

5. **Base64 Encoding**: Encrypted data is Base64 encoded, making it safe for:
   - URL parameters (with URL encoding)
   - JSON serialization
   - Database storage
   - XML attributes

6. **Do Not Mix Methods**: Data encrypted with `EncryptDeterministic` cannot be decrypted with `DecryptWithRandomIV` and vice versa.

7. **Machine Dependency**: Encrypted data can only be decrypted on the same machine (same system characteristics). For multi-server deployments, implement custom key storage.

8. **Performance**: Encryption operations are CPU-intensive. For high-throughput scenarios:
   - Cache decrypted values when possible
   - Consider encrypting only truly sensitive fields
   - Use async patterns to avoid blocking threads

9. **Compliance**: For PCI DSS, GDPR, HIPAA compliance:
    - Use `EncryptWithRandomIV` for all personal/sensitive data
    - Implement proper key management
    - Maintain audit logs of encryption/decryption operations
    - Consider additional layers of security (network encryption, disk encryption)
