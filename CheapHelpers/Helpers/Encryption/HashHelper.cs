using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CheapHelpers.Helpers.Encryption
{
    public static class HashHelper
    {
        #region MD5 Hashing

        /// <summary>
        /// Calculates MD5 hash from a byte array.
        /// </summary>
        /// <param name="input">Byte array to hash</param>
        /// <param name="format">Format string for byte conversion (default: "X2" for uppercase hex)</param>
        /// <returns>MD5 hash as hexadecimal string</returns>
        public static string CalculateMD5Hash(byte[] input, string format = "X2")
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(input);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString(format));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Calculates MD5 hash from a string.
        /// </summary>
        /// <param name="input">String to hash</param>
        /// <param name="format">Format string for byte conversion (default: "X2" for uppercase hex)</param>
        /// <returns>MD5 hash as hexadecimal string</returns>
        public static string CalculateMD5Hash(string input, string format = "X2")
        {
            var byteArray = Encoding.UTF8.GetBytes(input);
            return CalculateMD5Hash(byteArray, format);
        }

        #endregion

        #region FNV Hashing

        /// <summary>
        /// Calculates FNV (Fowler-Noll-Vo) hash for a gateway or path string.
        /// If a full path with pipe delimiter is provided, only the gateway portion (before first pipe) is hashed.
        /// Returns a hash value between 0-99 for distribution purposes.
        /// </summary>
        /// <param name="gatewayOrPath">Gateway name or full path with pipe delimiters</param>
        /// <returns>FNV hash value (0-99), or -1 on error</returns>
        public static long FNVHash(string gatewayOrPath)
        {
            try
            {
                // if fullpath, we only use part until first pipe (=gateway only)
                int pipeIndex = gatewayOrPath.IndexOf('|');
                if (pipeIndex > 0) gatewayOrPath = gatewayOrPath.Substring(0, pipeIndex);

                return FNVHashFull(gatewayOrPath);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Calculates full FNV (Fowler-Noll-Vo) hash implementation.
        /// Returns a hash value between 0-99 for distribution purposes.
        /// </summary>
        /// <param name="input">String to hash</param>
        /// <returns>FNV hash value (0-99)</returns>
        public static long FNVHashFull(string input)
        {
            const uint fnv_prime = 0x811C9DC5;
            uint hash = 0;
            int i = 0;

            for (i = 0; i < input.Length; i++)
            {
                hash *= fnv_prime;
                hash ^= ((byte)input[i]);
            }

            return hash % 100;
        }

        #endregion

        #region Hex String Conversions

        /// <summary>
        /// Converts a byte array to a hexadecimal string (lowercase).
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <returns>Hexadecimal string representation (lowercase, without 0x prefix)</returns>
        public static string ByteArrayToHexString(byte[] bytes)
        {
            return string.Join("", bytes.Select(x => x.ToString("x2")));
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array.
        /// </summary>
        /// <param name="hexString">Hexadecimal string (without 0x prefix)</param>
        /// <returns>Byte array representation of the hex string</returns>
        public static byte[] HexStringToByteArray(string hexString)
        {
            int length = hexString.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return bytes;
        }

        #endregion
    }
}
