using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CheapHelpers.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime GetDateTime(this TimeZoneInfo ti, DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTime(dateTime, ti);
        }

        public static DateTime GetDateTime(this DateTime dateTime, TimeZoneInfo ti)
        {
            return TimeZoneInfo.ConvertTime(dateTime, ti);
        }

        public static int GetWorkingDays(this DateTime current, DateTime finishDateExclusive, List<DateTime> excludedDates = null)
        {
            Func<int, bool> isWorkingDay = days =>
            {
                var currentDate = current.AddDays(days);
                var isNonWorkingDay =
                    currentDate.DayOfWeek == DayOfWeek.Saturday ||
                    currentDate.DayOfWeek == DayOfWeek.Sunday ||
                    excludedDates != null && excludedDates.Exists(excludedDate => excludedDate.Date.Equals(currentDate.Date));
                return !isNonWorkingDay;
            };

            return Enumerable.Range(0, (finishDateExclusive - current).Days).Count(isWorkingDay);
        }

        #region DateTimeOffset Extensions

        /// <summary>
        /// Rounds a DateTimeOffset down to the nearest interval.
        /// </summary>
        /// <param name="dto">The DateTimeOffset to round</param>
        /// <param name="rounding">The interval to round to</param>
        /// <returns>A DateTimeOffset rounded down to the nearest interval</returns>
        public static DateTimeOffset Floor(this DateTimeOffset dto, TimeSpan rounding)
        {
            long ticks = dto.Ticks / rounding.Ticks;
            return new DateTimeOffset(ticks * rounding.Ticks, TimeSpan.Zero);
        }

        /// <summary>
        /// Rounds a DateTimeOffset to the nearest interval.
        /// </summary>
        /// <param name="dto">The DateTimeOffset to round</param>
        /// <param name="rounding">The interval to round to</param>
        /// <returns>A DateTimeOffset rounded to the nearest interval</returns>
        public static DateTimeOffset Round(this DateTimeOffset dto, TimeSpan rounding)
        {
            long ticks = (dto.Ticks + (rounding.Ticks / 2) + 1) / rounding.Ticks;
            return new DateTimeOffset(ticks * rounding.Ticks, TimeSpan.Zero);
        }

        /// <summary>
        /// Rounds a DateTimeOffset up to the nearest interval.
        /// </summary>
        /// <param name="dto">The DateTimeOffset to round</param>
        /// <param name="rounding">The interval to round to</param>
        /// <returns>A DateTimeOffset rounded up to the nearest interval</returns>
        public static DateTimeOffset Ceiling(this DateTimeOffset dto, TimeSpan rounding)
        {
            long ticks = (dto.Ticks + rounding.Ticks - 1) / rounding.Ticks;
            return new DateTimeOffset(ticks * rounding.Ticks, TimeSpan.Zero);
        }

        /// <summary>
        /// Converts a DateTimeOffset to midnight UTC (zero time).
        /// </summary>
        /// <param name="timestamp">The timestamp to convert</param>
        /// <returns>A DateTimeOffset at midnight UTC for the same date</returns>
        public static DateTimeOffset ToZeroTime(this DateTimeOffset timestamp)
        {
            return new DateTimeOffset(timestamp.Date, TimeSpan.Zero);
        }

        /// <summary>
        /// Rounds a DateTimeOffset to a minute level (seconds set to zero).
        /// </summary>
        /// <param name="timestamp">The timestamp to round</param>
        /// <returns>A rounded DateTimeOffset at the minute level</returns>
        public static DateTimeOffset PerMinute(this DateTimeOffset timestamp)
        {
            return new DateTimeOffset(
                timestamp.Year,
                timestamp.Month,
                timestamp.Day,
                timestamp.Hour,
                timestamp.Minute,
                0,
                0,
                timestamp.Offset);
        }

        /// <summary>
        /// Rounds a DateTimeOffset to an hour level (minutes and seconds set to zero).
        /// </summary>
        /// <param name="timestamp">The timestamp to round</param>
        /// <returns>A rounded DateTimeOffset at the hour level</returns>
        public static DateTimeOffset PerHour(this DateTimeOffset timestamp)
        {
            return new DateTimeOffset(
                timestamp.Year,
                timestamp.Month,
                timestamp.Day,
                timestamp.Hour,
                0,
                0,
                0,
                timestamp.Offset);
        }

        /// <summary>
        /// Rounds a DateTimeOffset to a day level (time set to midnight).
        /// </summary>
        /// <param name="timestamp">The timestamp to round</param>
        /// <returns>A rounded DateTimeOffset at the day level</returns>
        public static DateTimeOffset PerDay(this DateTimeOffset timestamp)
        {
            return new DateTimeOffset(
                timestamp.Year,
                timestamp.Month,
                timestamp.Day,
                0,
                0,
                0,
                0,
                timestamp.Offset);
        }

        #endregion
    }
}
