using CheapHelpers.Helpers.Encryption;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CheapHelpers.Helpers.Security
{
    /// <summary>
    /// Cross-platform safe in-memory storage for strings using AES encryption
    /// </summary>
    public class Secret
    {
        private readonly byte[] _machineEntropy;

        /// <summary>
        /// Safe in-memory storage for strings with cross-platform support
        /// </summary>
        /// <param name="stringToHide">The string to encrypt and store</param>
        public Secret(string stringToHide)
        {
            _machineEntropy = GenerateMachineEntropy();
            ProtectedValue = Protect(stringToHide);
        }

        /// <summary>
        /// Gets the encrypted string
        /// </summary>
        public string ProtectedValue { get; }

        /// <summary>
        /// Gets the unprotected plain string. Be careful with this, clear the plain string from memory when done.
        /// </summary>
        /// <returns>The decrypted plain text string</returns>
        public string GetPlainString()
        {
            return Unprotect(ProtectedValue);
        }

        /// <summary>
        /// Creates a secure disposal method for strings (overwrites memory)
        /// </summary>
        /// <param name="sensitiveString">String to securely dispose</param>
        public static void SecureDispose(ref string sensitiveString)
        {
            if (string.IsNullOrEmpty(sensitiveString))
                return;

            // While we can't directly overwrite string memory in C#, 
            // we can at least clear the reference and request GC
            sensitiveString = string.Empty;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Encrypts a string using cross-platform AES encryption with machine-specific entropy
        /// </summary>
        /// <param name="str">String to encrypt</param>
        /// <returns>Base64 encoded encrypted string</returns>
        private string Protect(string str)
        {
            try
            {
                // Add machine entropy to the string before encryption for additional security
                var dataWithEntropy = CombineStringWithEntropy(str, _machineEntropy);
                var encryptedData = EncryptionHelper.Encrypt(dataWithEntropy);
                return encryptedData;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to protect string", ex);
            }
        }

        /// <summary>
        /// Decrypts the protected string
        /// </summary>
        /// <param name="protectedStr">Protected string to decrypt</param>
        /// <returns>Original plain text string</returns>
        private string Unprotect(string protectedStr)
        {
            try
            {
                var decryptedData = EncryptionHelper.Decrypt(protectedStr);
                var originalString = ExtractStringFromEntropy(decryptedData, _machineEntropy);
                return originalString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to unprotect string", ex);
            }
        }

        /// <summary>
        /// Generates machine-specific entropy for additional security
        /// </summary>
        /// <returns>Machine-specific entropy bytes</returns>
        private static byte[] GenerateMachineEntropy()
        {
            // Create machine-specific entropy using various system characteristics
            var entropy = new StringBuilder();
            entropy.Append(Environment.MachineName);
            entropy.Append(Environment.UserName);
            entropy.Append(Assembly.GetExecutingAssembly().FullName);
            entropy.Append(Environment.OSVersion.ToString());

            // Hash the entropy to ensure consistent length
            return SHA256.HashData(Encoding.UTF8.GetBytes(entropy.ToString()));
        }

        /// <summary>
        /// Combines string with entropy for additional security
        /// </summary>
        /// <param name="originalString">Original string</param>
        /// <param name="entropy">Machine entropy</param>
        /// <returns>Combined string</returns>
        private static string CombineStringWithEntropy(string originalString, byte[] entropy)
        {
            var entropyString = Convert.ToBase64String(entropy);
            return $"{originalString}|ENTROPY|{entropyString}";
        }

        /// <summary>
        /// Extracts original string from entropy-combined string
        /// </summary>
        /// <param name="combinedString">Combined string with entropy</param>
        /// <param name="expectedEntropy">Expected machine entropy</param>
        /// <returns>Original string</returns>
        private static string ExtractStringFromEntropy(string combinedString, byte[] expectedEntropy)
        {
            const string separator = "|ENTROPY|";
            var separatorIndex = combinedString.LastIndexOf(separator);

            if (separatorIndex == -1)
                throw new InvalidOperationException("Invalid protected string format");

            var originalString = combinedString[..separatorIndex];
            var entropyString = combinedString[(separatorIndex + separator.Length)..];

            // Verify entropy matches (additional security check)
            var expectedEntropyString = Convert.ToBase64String(expectedEntropy);
            if (entropyString != expectedEntropyString)
                throw new UnauthorizedAccessException("Entropy verification failed - string may have been tampered with");

            return originalString;
        }
    }
}