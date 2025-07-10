using System;
using System.Collections.Generic;
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
    }
}
