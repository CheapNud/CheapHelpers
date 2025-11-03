# EncryptionHelper

AES-256 encryption utilities with machine-specific key generation.

## Overview

The `EncryptionHelper` class provides AES-256-CBC encryption and decryption methods with two approaches:
- **Static IV** (deterministic encryption) - for URL parameters and cache keys
- **Random IV** (secure encryption) - for sensitive data (RECOMMENDED)

## Namespace

```csharp
using CheapHelpers.Helpers.Encryption;
```

## Security Model

The encryption key is **machine-specific** and **deterministic**, generated from:
- Machine name
- Username
- OS version
- Assembly name

This design allows encrypted data to be decrypted across sessions on the same machine without external key storage, but limits security against attackers who can determine these system characteristics.

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

### Encrypt (OBSOLETE - Static IV)

Encrypts data using a static, machine-specific IV.

**Signature:**
```csharp
[Obsolete("Use EncryptWithRandomIV() instead unless you specifically need deterministic encryption")]
public static string Encrypt(string plainText)
```

**Parameters:**
- `plainText`: Text to encrypt

**Returns:** Base64 encoded encrypted string

**Security Warning:** Uses static IV - same plaintext always produces same ciphertext.

**Valid Use Cases:**
- URL/route parameters that need to be matched or compared
- Cache keys or database lookups where deterministic encryption is required
- Scenarios needing same plaintext = same ciphertext for deduplication

**Example:**
```csharp
// URL parameter encryption (deterministic required)
string userId = "12345";
string encryptedId = EncryptionHelper.Encrypt(userId);
string url = $"https://example.com/user/{encryptedId}";

// Same user ID always produces same URL
string encryptedId2 = EncryptionHelper.Encrypt(userId);
// encryptedId == encryptedId2 (deterministic)
```

### Decrypt (OBSOLETE - Static IV)

Decrypts data that was encrypted with the static IV `Encrypt` method.

**Signature:**
```csharp
[Obsolete("Use DecryptWithRandomIV() instead unless you specifically need deterministic encryption")]
public static string Decrypt(string cipherText)
```

**Parameters:**
- `cipherText`: Base64 encoded encrypted text

**Returns:** Decrypted plain text string

**Throws:** `InvalidOperationException` if decryption fails

**Example:**
```csharp
string encryptedId = GetUrlParameter("id");
string userId = EncryptionHelper.Decrypt(encryptedId);
```

## Algorithm Details

**Encryption Standard:** AES-256-CBC
- **Key Size:** 256 bits (32 bytes)
- **IV Size:** 128 bits (16 bytes)
- **Mode:** Cipher Block Chaining (CBC)
- **Padding:** PKCS7

**Key Derivation:** PBKDF2-HMAC-SHA256
- **Iterations:** 10,000
- **Salt:** SHA256 hash of system characteristics

## Common Use Cases

### Storing Sensitive User Data

```csharp
public class UserService
{
    public async Task SaveUserCredentials(User user, string apiKey)
    {
        // RECOMMENDED: Use random IV for sensitive data
        user.EncryptedApiKey = EncryptionHelper.EncryptWithRandomIV(apiKey);
        await database.SaveAsync(user);
    }

    public async Task<string> GetUserApiKey(int userId)
    {
        var user = await database.GetUserAsync(userId);
        return EncryptionHelper.DecryptWithRandomIV(user.EncryptedApiKey);
    }
}
```

### Encrypted URL Parameters (Deterministic)

```csharp
public class UrlGenerator
{
    public string GenerateSecureUrl(int recordId)
    {
        // Use static IV for consistent URLs
        #pragma warning disable CS0618 // Type or member is obsolete
        string encryptedId = EncryptionHelper.Encrypt(recordId.ToString());
        #pragma warning restore CS0618

        return $"https://example.com/record/{encryptedId}";
    }

    public int DecodeRecordId(string encryptedId)
    {
        #pragma warning disable CS0618
        string decrypted = EncryptionHelper.Decrypt(encryptedId);
        #pragma warning restore CS0618

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
        #pragma warning disable CS0618
        string cacheKey = EncryptionHelper.Encrypt(key);
        #pragma warning restore CS0618

        _cache.Set(cacheKey, encrypted);
    }

    public string RetrieveSecureData(string key)
    {
        #pragma warning disable CS0618
        string cacheKey = EncryptionHelper.Encrypt(key);
        #pragma warning restore CS0618

        var encrypted = _cache.GetCachedItem(cacheKey);
        if (encrypted == null)
            return null;

        return EncryptionHelper.DecryptWithRandomIV(encrypted);
    }
}
```

### Configuration File Encryption

```csharp
public class ConfigManager
{
    public void SaveEncryptedConfig(string key, string value)
    {
        var encrypted = EncryptionHelper.EncryptWithRandomIV(value);

        var config = new Dictionary<string, string>
        {
            [key] = encrypted
        };

        File.WriteAllText("config.json", JsonConvert.SerializeObject(config));
    }

    public string LoadEncryptedConfig(string key)
    {
        var json = File.ReadAllText("config.json");
        var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        if (config.TryGetValue(key, out string encrypted))
        {
            return EncryptionHelper.DecryptWithRandomIV(encrypted);
        }

        return null;
    }
}
```

### Database Field Encryption

```csharp
public class PaymentMethodRepository
{
    public async Task SavePaymentMethod(PaymentMethod payment)
    {
        // Encrypt sensitive fields before saving
        payment.EncryptedCardNumber = EncryptionHelper.EncryptWithRandomIV(
            payment.CardNumber);

        payment.EncryptedCVV = EncryptionHelper.EncryptWithRandomIV(
            payment.CVV);

        // Clear plaintext data
        payment.CardNumber = null;
        payment.CVV = null;

        await database.SaveAsync(payment);
    }

    public async Task<PaymentMethod> GetPaymentMethod(int id)
    {
        var payment = await database.GetAsync(id);

        // Decrypt when needed
        payment.CardNumber = EncryptionHelper.DecryptWithRandomIV(
            payment.EncryptedCardNumber);

        payment.CVV = EncryptionHelper.DecryptWithRandomIV(
            payment.EncryptedCVV);

        return payment;
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
                // Decrypt with old method
                #pragma warning disable CS0618
                string plainText = EncryptionHelper.Decrypt(user.EncryptedApiKey);
                #pragma warning restore CS0618

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

3. **Production Security**: For production applications with high security requirements, consider:
   - Azure Key Vault for key storage
   - Windows DPAPI for Windows-specific applications
   - Hardware Security Modules (HSMs)
   - Key Management Services (KMS)

4. **Error Handling**: Always wrap decrypt operations in try-catch:
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

5. **Empty Strings**: Both methods return empty string for null or empty input without throwing exceptions.

6. **Base64 Encoding**: Encrypted data is Base64 encoded, making it safe for:
   - URL parameters (with URL encoding)
   - JSON serialization
   - Database storage
   - XML attributes

7. **Do Not Mix Methods**: Data encrypted with `Encrypt` cannot be decrypted with `DecryptWithRandomIV` and vice versa.

8. **Machine Dependency**: Encrypted data can only be decrypted on the same machine (same system characteristics). For multi-server deployments, implement custom key storage.

9. **Performance**: Encryption operations are CPU-intensive. For high-throughput scenarios:
   - Cache decrypted values when possible
   - Consider encrypting only truly sensitive fields
   - Use async patterns to avoid blocking threads

10. **Compliance**: For PCI DSS, GDPR, HIPAA compliance:
    - Use `EncryptWithRandomIV` for all personal/sensitive data
    - Implement proper key management
    - Maintain audit logs of encryption/decryption operations
    - Consider additional layers of security (network encryption, disk encryption)
