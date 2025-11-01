using System;
using System.IO;

namespace CheapHelpers.Extensions
{
    public static class BinaryReaderWriterExtensions
    {
        /// <summary>
        /// Writes a DateTimeOffset to a BinaryWriter by storing ticks and offset.
        /// </summary>
        /// <param name="writer">The BinaryWriter to write to</param>
        /// <param name="dto">The DateTimeOffset to write</param>
        public static void Write(this BinaryWriter writer, DateTimeOffset dto)
        {
            writer.Write(dto.Ticks);
            writer.Write(dto.Offset.Ticks);
        }

        /// <summary>
        /// Reads a DateTimeOffset from a BinaryReader.
        /// </summary>
        /// <param name="reader">The BinaryReader to read from</param>
        /// <returns>The DateTimeOffset that was read</returns>
        public static DateTimeOffset ReadDateTimeOffset(this BinaryReader reader)
        {
            var dt = reader.ReadInt64();
            var offset = reader.ReadInt64();
            return new DateTimeOffset(dt, TimeSpan.FromTicks(offset));
        }
    }
}
