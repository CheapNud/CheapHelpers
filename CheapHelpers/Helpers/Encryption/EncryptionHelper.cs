using System;
using System.IO;
using System.Security.Cryptography;

namespace CheapHelpers.Helpers.Encryption
{
    /// <summary>
    /// AES-256 obfuscation with a key derived from the machine name and the user name.
    /// The key is the same for every build of the library on that machine for that user, and anyone
    /// who knows those two values can rebuild it. Use it for obfuscation (encrypted route values,
    /// cache keys), not for secrets; for secrets use ASP.NET Core Data Protection or
    /// <see cref="CheapHelpers.Security.PassphraseEnvelope"/>.
    /// </summary>
    /// <remarks>
    /// Versions before this one also mixed in the OS version and the assembly name, so their ciphertext does not decrypt here.
    /// </remarks>
    public static class EncryptionHelper
    {
        private const int Pbkdf2Iterations = 10000;

        private static readonly Lazy<(byte[] Key, byte[] IV)> _keyIvPair = new(() => DeriveKeyAndIV(Environment.MachineName, Environment.UserName));

        /// <summary>Derives the AES key and the fixed IV from the machine name and the user name.</summary>
        internal static (byte[] Key, byte[] IV) DeriveKeyAndIV(string machineName, string userName)
        {
            // The separator keeps "ab" + "c" and "a" + "bc" from producing the same seed
            var seedHash = HashHelper.CalculateSHA256HashBytes(machineName + "\n" + userName);

            using var pbkdf2 = new Rfc2898DeriveBytes(seedHash, seedHash, Pbkdf2Iterations, HashAlgorithmName.SHA256);

            var key = pbkdf2.GetBytes(32); // 256-bit key for AES-256
            var iv = pbkdf2.GetBytes(16);  // 128-bit IV

            return (key, iv);
        }

        /// <summary>
        /// Encrypts using a deterministic (static) IV — same plaintext always produces the same ciphertext.
        /// Only use for URL/route parameters, cache keys, or deduplication where deterministic output is required.
        /// For general encryption, use <see cref="EncryptWithRandomIV"/> instead.
        /// </summary>
        public static string EncryptDeterministic(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var (key, iv) = _keyIvPair.Value;

            using var aes = Aes.Create();
            aes.KeySize = 256;
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
        /// Decrypts data encrypted with <see cref="EncryptDeterministic"/>.
        /// </summary>
        public static string DecryptDeterministic(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            var (key, iv) = _keyIvPair.Value;

            try
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
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
        /// <para>Note: For deterministic encryption (same plaintext = same ciphertext) for URL parameters or cache keys, use <see cref="EncryptDeterministic"/> instead.</para>
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