using System;
using System.Linq;
using System.Text;

namespace CheapHelpers.Helpers
{
    public static class BitHelper
    {
        /// <summary>
        /// Gets a bit value at a specific position from a byte.
        /// </summary>
        /// <param name="b">The byte to extract from</param>
        /// <param name="bitNumber">Bit position (0-7), counted from LSB(0) to MSB(7)</param>
        /// <returns>The bit value (0 or 1)</returns>
        public static int GetBit(byte b, int bitNumber)
        {
            return (b >> bitNumber) & 1;
        }

        /// <summary>
        /// Gets a bit value at a specific position from a short.
        /// </summary>
        /// <param name="b">The short to extract from</param>
        /// <param name="bitNumber">Bit position, counted from LSB to MSB</param>
        /// <returns>The bit value (0 or 1)</returns>
        public static int GetBit(short b, int bitNumber)
        {
            return (b >> bitNumber) & 1;
        }

        /// <summary>
        /// Gets a bit value at a specific position from an unsigned short.
        /// </summary>
        /// <param name="b">The ushort to extract from</param>
        /// <param name="bitNumber">Bit position, counted from LSB to MSB</param>
        /// <returns>The bit value (0 or 1)</returns>
        public static int GetBit(ushort b, int bitNumber)
        {
            return (b >> bitNumber) & 1;
        }

        /// <summary>
        /// Concatenates multiple bytes into a single integer value.
        /// </summary>
        /// <param name="bytes">1-4 bytes to concatenate</param>
        /// <returns>Integer value created from concatenated bytes</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if more than 4 bytes or less than 1 byte provided</exception>
        public static int ConcatBytesToInt(params byte[] bytes)
        {
            if (bytes.Length > 4) { throw new ArgumentOutOfRangeException("only 1 to 4 bytes allowed to avoid dataloss in converting to int"); }
            if (bytes.Length < 1) { throw new ArgumentOutOfRangeException("provide at least a single byte"); }
            int returnValue = bytes[0];
            for (int i = 1; i < bytes.Length; i++)
            {
                returnValue = returnValue << 8 | bytes[i];
            }
            return returnValue;
        }

        #region Little-Endian Parsing

        /// <summary>
        /// Parses a little-endian Int16 from a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer</param>
        /// <param name="offset">Offset in the buffer to start reading</param>
        /// <returns>Parsed Int16 value</returns>
        public static short ParseLittleEndianInt16(byte[] buffer, int offset)
        {
            return (short)((buffer[offset + 1] << 8) + buffer[offset]);
        }

        /// <summary>
        /// Parses a little-endian UInt16 from a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer</param>
        /// <param name="offset">Offset in the buffer to start reading</param>
        /// <returns>Parsed UInt16 value</returns>
        public static ushort ParseLittleEndianUInt16(byte[] buffer, int offset)
        {
            return (ushort)((buffer[offset + 1] << 8) + buffer[offset]);
        }

        /// <summary>
        /// Parses specific bits from a little-endian Int16 in a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer</param>
        /// <param name="offset">Offset in the buffer to start reading</param>
        /// <param name="bitOffset">Bit offset within the Int16</param>
        /// <param name="bitLength">Number of bits to extract</param>
        /// <returns>Parsed bit value as Int16</returns>
        public static short ParseLittleEndianInt16Bits(byte[] buffer, int offset, int bitOffset, int bitLength)
        {
            var temp = ParseLittleEndianInt16(buffer, offset);
            temp >>= bitOffset;
            var mask = 0xffff >> (16 - bitLength);
            return (short)(temp & mask);
        }

        /// <summary>
        /// Parses specific bits from a little-endian UInt16 in a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer</param>
        /// <param name="offset">Offset in the buffer to start reading</param>
        /// <param name="bitOffset">Bit offset within the UInt16</param>
        /// <param name="bitLength">Number of bits to extract</param>
        /// <returns>Parsed bit value as UInt16</returns>
        public static ushort ParseLittleEndianUInt16Bits(byte[] buffer, int offset, int bitOffset, int bitLength)
        {
            var temp = ParseLittleEndianUInt16(buffer, offset);
            temp >>= bitOffset;
            var mask = 0xffff >> (16 - bitLength);
            return (ushort)(temp & mask);
        }

        /// <summary>
        /// Parses a little-endian Int32 from a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer</param>
        /// <param name="offset">Offset in the buffer to start reading</param>
        /// <returns>Parsed Int32 value</returns>
        public static int ParseLittleEndianInt32(byte[] buffer, int offset)
        {
            return (buffer[offset + 3] << 24) + (buffer[offset + 2] << 16) + (buffer[offset + 1] << 8) + buffer[offset];
        }

        /// <summary>
        /// Parses a little-endian UInt32 from a byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer</param>
        /// <param name="offset">Offset in the buffer to start reading</param>
        /// <returns>Parsed UInt32 value</returns>
        public static uint ParseLittleEndianUInt32(byte[] buffer, int offset)
        {
            return (uint)((buffer[offset + 3] << 24) + (buffer[offset + 2] << 16) + (buffer[offset + 1] << 8) + buffer[offset]);
        }

        #endregion

        #region Hex String Conversions

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
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array using LINQ.
        /// </summary>
        /// <param name="hex">Hexadecimal string (without 0x prefix)</param>
        /// <returns>Byte array representation of the hex string</returns>
        public static byte[] HexStringToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
        }

        /// <summary>
        /// Converts a byte array to a hexadecimal string.
        /// </summary>
        /// <param name="ba">Byte array to convert</param>
        /// <returns>Hexadecimal string representation (lowercase, without 0x prefix)</returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        #endregion
    }
}
