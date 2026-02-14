using System.Globalization;

namespace CheapHelpers.Extensions
{
    public static class NumericExtensions
    {
        private static readonly string[] FileSizeUnits = ["B", "KB", "MB", "GB", "TB"];

        /// <summary>
        /// Converts a byte count to a human-readable file size string.
        /// Automatically selects the most appropriate unit (B, KB, MB, GB, TB).
        /// </summary>
        /// <param name="bytes">The byte count to format.</param>
        /// <returns>A formatted string like "1.50 GB" or "250.00 MB".</returns>
        /// <example>
        /// 1024L.ToReadableFileSize() returns "1.00 KB"
        /// 1_610_612_736L.ToReadableFileSize() returns "1.50 GB"
        /// 0L.ToReadableFileSize() returns "0.00 B"
        /// </example>
        public static string ToReadableFileSize(this long bytes)
        {
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < FileSizeUnits.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return string.Create(CultureInfo.InvariantCulture, $"{len:F2} {FileSizeUnits[order]}");
        }
    }
}
