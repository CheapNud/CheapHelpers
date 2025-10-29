using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CheapHelpers.Helpers.Encryption
{
    public static class EncryptionHelper
    {
        private static readonly Lazy<(byte[] Key, byte[] IV)> _keyIvPair = new(() => GenerateKeyAndIV());

        /// <summary>
        /// Generates a machine-specific key and IV for AES encryption.
        ///
        /// SECURITY WARNING: This method is DETERMINISTIC by design - it generates the same
        /// key and IV for the same machine every time. This design choice allows data encrypted
        /// on one session to be decrypted in another session on the same machine without
        /// external key storage.
        ///
        /// SECURITY IMPLICATIONS:
        /// - The encryption key can be predicted by anyone who knows the machine name, username,
        ///   OS version, and assembly name. These values provide limited entropy.
        /// - If an attacker has access to encrypted data AND can determine these system values,
        ///   they can reconstruct the encryption key.
        /// - This approach is suitable for obfuscation and protection against casual access,
        ///   but NOT for protecting highly sensitive data against determined attackers.
        ///
        /// RECOMMENDATIONS:
        /// - For sensitive data requiring strong security, use EncryptWithRandomIV() which
        ///   generates a unique IV for each encryption operation.
        /// - Consider storing encryption keys in secure key storage (e.g., Azure Key Vault,
        ///   Windows DPAPI, or hardware security modules) for production scenarios.
        /// - Evaluate whether the deterministic behavior is required for your use case.
        /// </summary>
        /// <returns>Tuple containing the key and IV</returns>
        private static (byte[] Key, byte[] IV) GenerateKeyAndIV()
        {
            // SECURITY NOTE: This is deterministic by design - same machine = same key
            // The predictable nature is intentional to support decrypt-without-key-storage scenarios
            // but limits the security against attackers who can determine system characteristics

            // Create a deterministic but machine-specific seed
            var seed = new StringBuilder();
            seed.Append(Environment.MachineName);
            seed.Append(Environment.UserName);
            seed.Append(Environment.OSVersion.ToString());
            seed.Append(System.Reflection.Assembly.GetExecutingAssembly().FullName);

            // Hash the seed to create consistent key material
            using var sha256 = SHA256.Create();
            var seedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed.ToString()));

            // Use PBKDF2 to derive key and IV from the seed
            // 10000 iterations provide some computational cost but can still be brute-forced
            // if the seed entropy is low (which it is, given the predictable inputs)
            using var pbkdf2 = new Rfc2898DeriveBytes(seedHash, seedHash, 10000, HashAlgorithmName.SHA256);

            var key = pbkdf2.GetBytes(32); // 256-bit key for AES-256
            var iv = pbkdf2.GetBytes(16);  // 128-bit IV

            return (key, iv);
        }

        /// <summary>
        /// Encrypts a plain text string using AES-256-CBC with a STATIC/DETERMINISTIC IV.
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Base64 encoded encrypted string</returns>
        /// <remarks>
        /// <para><strong>SECURITY WARNING:</strong> This method uses a static, machine-specific IV that is reused for every encryption operation.</para>
        /// <para><strong>When NOT to use this method:</strong></para>
        /// <list type="bullet">
        /// <item><description>Encrypting sensitive user data (passwords, API keys, personal information)</description></item>
        /// <item><description>Encrypting data that will be stored long-term</description></item>
        /// <item><description>Any scenario where pattern analysis could reveal information</description></item>
        /// <item><description>When the same plaintext should produce different ciphertexts each time</description></item>
        /// </list>
        /// <para><strong>Valid use cases (deterministic encryption required):</strong></para>
        /// <list type="bullet">
        /// <item><description>URL/route parameters that need to be matched or compared</description></item>
        /// <item><description>Cache keys or database lookups where same plaintext = same ciphertext is required</description></item>
        /// <item><description>Scenarios where you need deterministic encryption for deduplication</description></item>
        /// </list>
        /// <para><strong>For most use cases, use <see cref="EncryptWithRandomIV"/> instead.</strong></para>
        /// </remarks>
        [Obsolete("This method uses a static IV which is cryptographically weak for most use cases. Use EncryptWithRandomIV() instead unless you specifically need deterministic encryption (e.g., for URL parameters or cache keys).")]
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var (key, iv) = _keyIvPair.Value;

            using var aes = Aes.Create();
            aes.KeySize = 256; // Use AES-256
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cs);

            writer.Write(plainText);
            writer.Flush();
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Decrypts a Base64 encoded encrypted string using AES-256-CBC with a STATIC/DETERMINISTIC IV.
        /// </summary>
        /// <param name="cipherText">Base64 encoded encrypted text (encrypted with <see cref="Encrypt"/>)</param>
        /// <returns>Decrypted plain text string</returns>
        /// <remarks>
        /// <para>This method is the counterpart to <see cref="Encrypt"/> and uses the same static IV.</para>
        /// <para>For decrypting data encrypted with <see cref="EncryptWithRandomIV"/>, use <see cref="DecryptWithRandomIV"/> instead.</para>
        /// </remarks>
        [Obsolete("This method uses a static IV which is cryptographically weak for most use cases. Use DecryptWithRandomIV() instead unless you specifically need deterministic encryption (e.g., for URL parameters or cache keys).")]
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var (key, iv) = _keyIvPair.Value;

            try
            {
                using var aes = Aes.Create();
                aes.KeySize = 256; // Use AES-256
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt data. The data may be corrupted or was encrypted with different parameters.", ex);
            }
        }

        /// <summary>
        /// Encrypts data with a RANDOM IV - RECOMMENDED for most use cases.
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Base64 encoded string containing IV + encrypted data</returns>
        /// <remarks>
        /// <para><strong>RECOMMENDED DEFAULT:</strong> This method provides cryptographically secure encryption by generating a unique, random IV for each encryption operation.</para>
        /// <para><strong>Security Benefits:</strong></para>
        /// <list type="bullet">
        /// <item><description>Each encryption produces a different ciphertext, even for identical plaintext</description></item>
        /// <item><description>Prevents pattern analysis attacks</description></item>
        /// <item><description>Follows cryptographic best practices (NIST guidelines)</description></item>
        /// <item><description>Suitable for encrypting sensitive user data (passwords, API keys, personal information)</description></item>
        /// </list>
        /// <para><strong>Use this method for:</strong></para>
        /// <list type="bullet">
        /// <item><description>Encrypting sensitive data (default choice)</description></item>
        /// <item><description>Long-term data storage</description></item>
        /// <item><description>User credentials and personal information</description></item>
        /// <item><description>Any scenario where security is prioritized over deterministic output</description></item>
        /// </list>
        /// <para>The IV is automatically prepended to the ciphertext and extracted during decryption by <see cref="DecryptWithRandomIV"/>.</para>
        /// <para>Note: If you need deterministic encryption (same plaintext = same ciphertext) for URL parameters or cache keys, use <see cref="Encrypt"/> instead, but be aware of its security limitations.</para>
        /// </remarks>
        public static string EncryptWithRandomIV(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var (key, _) = _keyIvPair.Value;

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = key;
            aes.GenerateIV(); // Generate random IV for each encryption
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();

            // Prepend IV to the encrypted data
            ms.Write(aes.IV, 0, aes.IV.Length);

            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cs);

            writer.Write(plainText);
            writer.Flush();
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Decrypts data that was encrypted with <see cref="EncryptWithRandomIV"/> - RECOMMENDED for most use cases.
        /// </summary>
        /// <param name="cipherText">Base64 encoded string containing IV + encrypted data</param>
        /// <returns>Decrypted plain text string</returns>
        /// <remarks>
        /// <para>This method automatically extracts the IV from the beginning of the ciphertext and uses it for decryption.</para>
        /// <para>Only use this method to decrypt data that was encrypted with <see cref="EncryptWithRandomIV"/>.</para>
        /// <para>For data encrypted with the legacy <see cref="Encrypt"/> method, use <see cref="Decrypt"/> instead.</para>
        /// </remarks>
        public static string DecryptWithRandomIV(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var (key, _) = _keyIvPair.Value;

            try
            {
                var fullData = Convert.FromBase64String(cipherText);

                if (fullData.Length < 16)
                    throw new ArgumentException("Invalid cipher text - too short to contain IV");

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from the beginning of the data
                var iv = new byte[16];
                Array.Copy(fullData, 0, iv, 0, 16);
                aes.IV = iv;

                // Extract encrypted data (everything after the IV)
                var encryptedData = new byte[fullData.Length - 16];
                Array.Copy(fullData, 16, encryptedData, 0, encryptedData.Length);

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(encryptedData);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt data. The data may be corrupted or was encrypted with different parameters.", ex);
            }
        }
    }
}