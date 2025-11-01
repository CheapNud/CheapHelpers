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
        /// <param name="filename">The filename (with or without extension)</param>
        /// <param name="timestamp">Optional timestamp to include in the filename</param>
        /// <param name="dateFormat">Optional date format pattern (e.g., "yyyy-MM-dd"). If timestamp is provided but format is null, uses default format.</param>
        /// <returns>A secure filename with format: {filename}[_{formattedDate}]_{guid}[.{extension}]</returns>
        public static string GetTrustedFileName(string filename, DateTime? timestamp = null, string? dateFormat = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);

            // Extract extension from filename
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);

            // Build the filename
            var result = filenameWithoutExt;

            // Add formatted timestamp if provided
            if (timestamp.HasValue)
            {
                var formattedDate = string.IsNullOrWhiteSpace(dateFormat)
                    ? timestamp.Value.ToString()
                    : timestamp.Value.ToString(dateFormat);
                result = $"{result}_{formattedDate}";
            }

            // Add GUID suffix for security
            result = $"{result}_{Guid.NewGuid().ToString("N")[..8]}";

            // Add extension if present
            if (!string.IsNullOrEmpty(extension))
            {
                result = $"{result}{extension}";
            }

            return result;
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
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy-MM-dd_{guid}[.{extension}]</returns>
        public static string GetDailyFilename(DateTime timestamp, string filename)
            => GetTrustedFileName(filename, timestamp, "yyyy-MM-dd");

        /// <summary>
        /// Generates a secure filename with daily pattern (yyyy-MM-dd) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy-MM-dd_{guid}[.{extension}]</returns>
        public static string GetDailyFilename(DateTimeOffset timestamp, string filename)
            => GetDailyFilename(timestamp.DateTime, filename);

        /// <summary>
        /// Generates a secure filename with weekly pattern (yyyy-wN where N is week number) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy-wN_{guid}[.{extension}]</returns>
        public static string GetWeeklyFilename(DateTime timestamp, string filename)
        {
            // Note: Cannot use GetTrustedFileName's dateFormat parameter because DateTime.ToString()
            // doesn't support week-of-year formatting. We manually calculate ISO week number and
            // pre-build the filename string instead.
            var weekNumber = DateTimeFormatInfo.InvariantInfo.Calendar.GetWeekOfYear(
                timestamp,
                CalendarWeekRule.FirstDay,
                DayOfWeek.Monday);

            var weekPattern = $"{timestamp.Year}-w{weekNumber}";
            return GetTrustedFileName($"{Path.GetFileNameWithoutExtension(filename)}_{weekPattern}{Path.GetExtension(filename)}");
        }

        /// <summary>
        /// Generates a secure filename with weekly pattern (yyyy-wN where N is week number) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy-wN_{guid}[.{extension}]</returns>
        public static string GetWeeklyFilename(DateTimeOffset timestamp, string filename)
            => GetWeeklyFilename(timestamp.DateTime, filename);

        /// <summary>
        /// Generates a secure filename with monthly pattern (yyyy-MM) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy-MM_{guid}[.{extension}]</returns>
        public static string GetMonthlyFilename(DateTime timestamp, string filename)
            => GetTrustedFileName(filename, timestamp, "yyyy-MM");

        /// <summary>
        /// Generates a secure filename with monthly pattern (yyyy-MM) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy-MM_{guid}[.{extension}]</returns>
        public static string GetMonthlyFilename(DateTimeOffset timestamp, string filename)
            => GetMonthlyFilename(timestamp.DateTime, filename);

        /// <summary>
        /// Generates a secure filename with yearly pattern (yyyy) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy_{guid}[.{extension}]</returns>
        public static string GetYearlyFilename(DateTime timestamp, string filename)
            => GetTrustedFileName(filename, timestamp, "yyyy");

        /// <summary>
        /// Generates a secure filename with yearly pattern (yyyy) and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <returns>A secure filename in format: {filename}_yyyy_{guid}[.{extension}]</returns>
        public static string GetYearlyFilename(DateTimeOffset timestamp, string filename)
            => GetYearlyFilename(timestamp.DateTime, filename);

        /// <summary>
        /// Generates a secure custom date-based filename with specified format pattern and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <param name="dateFormat">Custom date format pattern (e.g., "yyyyMMdd", "yyyy-MM-dd-HHmmss")</param>
        /// <returns>A secure filename in format: {filename}_{datePattern}_{guid}[.{extension}]</returns>
        public static string GetCustomDateFilename(DateTime timestamp, string filename, string dateFormat)
            => GetTrustedFileName(filename, timestamp, dateFormat);

        /// <summary>
        /// Generates a secure custom date-based filename with specified format pattern and GUID suffix.
        /// </summary>
        /// <param name="timestamp">The timestamp to use for the filename</param>
        /// <param name="filename">The filename (with or without extension)</param>
        /// <param name="dateFormat">Custom date format pattern (e.g., "yyyyMMdd", "yyyy-MM-dd-HHmmss")</param>
        /// <returns>A secure filename in format: {filename}_{datePattern}_{guid}[.{extension}]</returns>
        public static string GetCustomDateFilename(DateTimeOffset timestamp, string filename, string dateFormat)
            => GetCustomDateFilename(timestamp.DateTime, filename, dateFormat);

        #endregion
    }
}
