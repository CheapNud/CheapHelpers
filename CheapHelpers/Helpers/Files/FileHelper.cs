using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CheapHelpers.Helpers.Files
{
    public static class FileHelper
    {
        /// <summary>
        /// Generates a secure filename with GUID suffix to prevent file overwrite attacks.
        /// Optionally includes a formatted date/time pattern.
        /// </summary>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="timestamp">Optional timestamp to include in the filename</param>
        /// <param name="dateFormat">Optional date format pattern (e.g., "yyyy-MM-dd"). If timestamp is provided but format is null, uses default format.</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, attempts to extract from baseName.</param>
        /// <returns>A secure filename with format: {baseName}[_{formattedDate}]_{guid}[.{extension}]</returns>
        public static string GetTrustedFileName(string baseName, DateTime? timestamp = null, string? dateFormat = null, string? extension = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseName);

            // Extract extension from baseName if not explicitly provided
            var baseNameWithoutExt = Path.GetFileNameWithoutExtension(baseName);
            var extractedExt = Path.GetExtension(baseName);

            // Use provided extension, or fall back to extracted extension
            var finalExtension = extension ?? (string.IsNullOrEmpty(extractedExt) ? null : extractedExt.TrimStart('.'));

            // Build the filename
            var filename = baseNameWithoutExt;

            // Add formatted timestamp if provided
            if (timestamp.HasValue)
            {
                var formattedDate = string.IsNullOrWhiteSpace(dateFormat)
                    ? timestamp.Value.ToString()
                    : timestamp.Value.ToString(dateFormat);
                filename = $"{filename}_{formattedDate}";
            }

            // Add GUID suffix for security
            filename = $"{filename}_{Guid.NewGuid().ToString("N")[..8]}";

            // Add extension if present
            if (!string.IsNullOrWhiteSpace(finalExtension))
            {
                finalExtension = finalExtension.TrimStart('.');
                filename = $"{filename}.{finalExtension}";
            }

            return filename;
        }

        public static string GetTrustedFileName(FileInfo file)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentException.ThrowIfNullOrWhiteSpace(file.Name);

            return GetTrustedFileName(file.Name);
        }

        public static string GetTrustedFileNameFromPath(string filepath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filepath);

            return GetTrustedFileName(new FileInfo(filepath));
        }

        public static string GetTrustedFileNameFromTempPath(string filename)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);

            return GetTrustedFileNameFromPath(Path.Combine(Path.GetTempPath(), filename));
        }

        public static string ChangeFileNameId(string filename)
        {
            var arr = Path.GetFileNameWithoutExtension(filename).Split('_');

            var filenamewoid = filename.Replace(@$"_{arr.Last()}", "");

            var a = @$"{filenamewoid}{Path.GetExtension(filename)}";
            return GetTrustedFileName(a);
        }

        #region Date-Based Filename Generation

        /// <summary>
        /// Generates a secure filename with daily pattern (yyyy-MM-dd) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy-MM-dd_{guid}.{extension}</returns>
        public static string GetDailyFilename(DateTime timestamp, string baseName, string? extension = null)
            => GetTrustedFileName(baseName, timestamp, "yyyy-MM-dd", extension);

        /// <summary>
        /// Generates a secure filename with daily pattern (yyyy-MM-dd) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy-MM-dd_{guid}.{extension}</returns>
        public static string GetDailyFilename(DateTimeOffset timestamp, string baseName, string? extension = null)
            => GetDailyFilename(timestamp.DateTime, baseName, extension);

        /// <summary>
        /// Generates a secure filename with weekly pattern (yyyy-wN where N is week number) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy-wN_{guid}.{extension}</returns>
        public static string GetWeeklyFilename(DateTime timestamp, string baseName, string? extension = null)
        {
            var weekNumber = DateTimeFormatInfo.InvariantInfo.Calendar.GetWeekOfYear(
                timestamp,
                CalendarWeekRule.FirstDay,
                DayOfWeek.Monday);

            var weekPattern = $"{timestamp.Year}-w{weekNumber}";
            return GetTrustedFileName($"{baseName}_{weekPattern}", null, null, extension);
        }

        /// <summary>
        /// Generates a secure filename with weekly pattern (yyyy-wN where N is week number) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy-wN_{guid}.{extension}</returns>
        public static string GetWeeklyFilename(DateTimeOffset timestamp, string baseName, string? extension = null)
            => GetWeeklyFilename(timestamp.DateTime, baseName, extension);

        /// <summary>
        /// Generates a secure filename with monthly pattern (yyyy-MM) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy-MM_{guid}.{extension}</returns>
        public static string GetMonthlyFilename(DateTime timestamp, string baseName, string? extension = null)
            => GetTrustedFileName(baseName, timestamp, "yyyy-MM", extension);

        /// <summary>
        /// Generates a secure filename with monthly pattern (yyyy-MM) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy-MM_{guid}.{extension}</returns>
        public static string GetMonthlyFilename(DateTimeOffset timestamp, string baseName, string? extension = null)
            => GetMonthlyFilename(timestamp.DateTime, baseName, extension);

        /// <summary>
        /// Generates a secure filename with yearly pattern (yyyy) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy_{guid}.{extension}</returns>
        public static string GetYearlyFilename(DateTime timestamp, string baseName, string? extension = null)
            => GetTrustedFileName(baseName, timestamp, "yyyy", extension);

        /// <summary>
        /// Generates a secure filename with yearly pattern (yyyy) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_yyyy_{guid}.{extension}</returns>
        public static string GetYearlyFilename(DateTimeOffset timestamp, string baseName, string? extension = null)
            => GetYearlyFilename(timestamp.DateTime, baseName, extension);

        /// <summary>
        /// Generates a secure custom date-based filename with specified format pattern and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="dateFormat">Custom date format pattern (e.g., "yyyyMMdd", "yyyy-MM-dd-HHmmss")</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_{datePattern}_{guid}.{extension}</returns>
        public static string GetCustomDateFilename(DateTime timestamp, string baseName, string dateFormat, string? extension = null)
            => GetTrustedFileName(baseName, timestamp, dateFormat, extension);

        /// <summary>
        /// Generates a secure custom date-based filename with specified format pattern and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="baseName">The base name for the file (without extension)</param>
        /// <param name="dateFormat">Custom date format pattern (e.g., "yyyyMMdd", "yyyy-MM-dd-HHmmss")</param>
        /// <param name="extension">Optional file extension (without leading dot). If null, no extension is added.</param>
        /// <returns>A secure filename in format: {baseName}_{datePattern}_{guid}.{extension}</returns>
        public static string GetCustomDateFilename(DateTimeOffset timestamp, string baseName, string dateFormat, string? extension = null)
            => GetCustomDateFilename(timestamp.DateTime, baseName, dateFormat, extension);

        #endregion
    }
}
