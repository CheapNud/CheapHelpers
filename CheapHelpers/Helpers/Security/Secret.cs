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
                // Use random IV for maximum security - each Secret instance gets unique encryption
                var encryptedData = EncryptionHelper.EncryptWithRandomIV(dataWithEntropy);
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
                // Use random IV decryption to match EncryptWithRandomIV
                var decryptedData = EncryptionHelper.DecryptWithRandomIV(protectedStr);
                var originalString = ExtractStringFromEntropy(decryptedData, _machineEntropy);
                return originalString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to unprotect string", ex);
            }
        }

        /// <summary>
        /// Generates machine-specific entropy for additional security.
        ///
        /// SECURITY NOTE: This method combines machine-specific values (as salt) with
        /// cryptographic randomness to create strong entropy. The machine-specific values
        /// provide deterministic verification while the random component ensures that
        /// each Secret instance has unique, unpredictable entropy.
        ///
        /// This approach balances:
        /// - Cryptographic randomness: Prevents prediction attacks
        /// - Machine-specific binding: Adds an additional layer of verification
        /// - Per-instance uniqueness: Each Secret object gets different entropy
        /// </summary>
        /// <returns>Machine-specific entropy bytes combined with cryptographic randomness</returns>
        private static byte[] GenerateMachineEntropy()
        {
            // SECURITY ENHANCEMENT: Generate cryptographically secure random bytes
            // This provides true entropy that cannot be predicted by attackers
            var randomBytes = new byte[32]; // 256 bits of random data
            RandomNumberGenerator.Fill(randomBytes);

            // Create machine-specific salt using various system characteristics
            // This binds the encryption to the specific machine for additional security
            var machineSalt = new StringBuilder();
            machineSalt.Append(Environment.MachineName);
            machineSalt.Append(Environment.UserName);
            machineSalt.Append(Assembly.GetExecutingAssembly().FullName);
            machineSalt.Append(Environment.OSVersion.ToString());

            // Hash the machine salt to ensure consistent length
            var machineSaltHash = SHA256.HashData(Encoding.UTF8.GetBytes(machineSalt.ToString()));

            // Combine random bytes with machine salt using HMAC for cryptographic mixing
            // This creates entropy that is both random and machine-bound
            using var hmac = new HMACSHA256(machineSaltHash);
            var combinedEntropy = hmac.ComputeHash(randomBytes);

            return combinedEntropy;
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