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
        /// Generates a machine-specific key and IV for AES encryption
        /// </summary>
        /// <returns>Tuple containing the key and IV</returns>
        private static (byte[] Key, byte[] IV) GenerateKeyAndIV()
        {
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
            using var pbkdf2 = new Rfc2898DeriveBytes(seedHash, seedHash, 10000, HashAlgorithmName.SHA256);

            var key = pbkdf2.GetBytes(32); // 256-bit key for AES-256
            var iv = pbkdf2.GetBytes(16);  // 128-bit IV

            return (key, iv);
        }

        /// <summary>
        /// Encrypts a plain text string using AES-256-CBC
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Base64 encoded encrypted string</returns>
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
        /// Decrypts a Base64 encoded encrypted string using AES-256-CBC
        /// </summary>
        /// <param name="cipherText">Base64 encoded encrypted text</param>
        /// <returns>Decrypted plain text string</returns>
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
        /// Encrypts data with a random IV (more secure for each encryption operation)
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Base64 encoded string containing IV + encrypted data</returns>
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
        /// Decrypts data that was encrypted with EncryptWithRandomIV
        /// </summary>
        /// <param name="cipherText">Base64 encoded string containing IV + encrypted data</param>
        /// <returns>Decrypted plain text string</returns>
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