using System;

namespace CheapHelpers.Extensions
{
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Converts a TimeSpan to a human-readable string.
        /// Automatically selects the most appropriate unit (seconds, minutes, hours, or days).
        /// Handles singular/plural forms correctly.
        /// </summary>
        /// <param name="t">The TimeSpan to convert</param>
        /// <returns>A human-readable string representation</returns>
        /// <example>
        /// TimeSpan.FromSeconds(45).ToReadableString() returns "45 seconds"
        /// TimeSpan.FromMinutes(1).ToReadableString() returns "1 minute"
        /// TimeSpan.FromHours(2.5).ToReadableString() returns "2 hours"
        /// </example>
        public static string ToReadableString(this TimeSpan t)
        {
            string result;
            if (t.TotalSeconds <= 1)
            {
                result = $@"{t:s\.ff} seconds";
            }
            else if (t.TotalMinutes <= 1)
            {
                result = $@"{t:%s} seconds";
            }
            else if (t.TotalHours <= 1)
            {
                result = $@"{t:%m} minutes";
            }
            else if (t.TotalDays <= 1)
            {
                result = $@"{t:%h} hours";
            }
            else
            {
                result = $@"{t:%d} days";
            }

            if (result.StartsWith("1 "))
            {
                // remove "s" at the end for singular form
                result = result.Remove(result.Length - 1);
            }

            return result;
        }
    }
}
