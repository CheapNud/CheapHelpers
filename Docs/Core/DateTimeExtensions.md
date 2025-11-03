# DateTimeExtensions

DateTime and DateTimeOffset manipulation and rounding extension methods.

## Overview

The `DateTimeExtensions` class provides extension methods for working with `DateTime`, `DateTimeOffset`, and `TimeZoneInfo`. Features include timezone conversions, working day calculations, and temporal rounding operations.

## Namespace

```csharp
using CheapHelpers.Extensions;
```

## Methods

### GetDateTime (TimeZoneInfo Extension)

Converts a DateTime to a specific timezone.

**Signature:**
```csharp
public static DateTime GetDateTime(this TimeZoneInfo ti, DateTime dateTime)
```

**Parameters:**
- `ti`: The target timezone
- `dateTime`: The DateTime to convert

**Returns:** DateTime converted to the specified timezone

**Example:**
```csharp
var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
DateTime localTime = DateTime.Now;
DateTime easternTime = eastern.GetDateTime(localTime);
```

### GetDateTime (DateTime Extension)

Converts a DateTime to a specific timezone (alternative syntax).

**Signature:**
```csharp
public static DateTime GetDateTime(this DateTime dateTime, TimeZoneInfo ti)
```

**Parameters:**
- `dateTime`: The DateTime to convert
- `ti`: The target timezone

**Returns:** DateTime converted to the specified timezone

**Example:**
```csharp
var pacific = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
DateTime utcTime = DateTime.UtcNow;
DateTime pacificTime = utcTime.GetDateTime(pacific);
```

### GetWorkingDays

Calculates the number of working days (excluding weekends and specified excluded dates) between two dates.

**Signature:**
```csharp
public static int GetWorkingDays(this DateTime current, DateTime finishDateExclusive, List<DateTime> excludedDates = null)
```

**Parameters:**
- `current`: Start date (inclusive)
- `finishDateExclusive`: End date (exclusive)
- `excludedDates`: Optional list of dates to exclude (e.g., holidays)

**Returns:** Number of working days between the dates

**Working Day Definition:**
- Excludes Saturdays and Sundays
- Excludes any dates in the `excludedDates` list

**Example:**
```csharp
DateTime start = new DateTime(2024, 1, 1);  // Monday
DateTime end = new DateTime(2024, 1, 8);    // Following Monday

// Without holidays (5 working days: Mon-Fri)
int workDays = start.GetWorkingDays(end);
// Result: 5

// With holidays
var holidays = new List<DateTime>
{
    new DateTime(2024, 1, 1)  // New Year's Day
};
int workDaysWithHoliday = start.GetWorkingDays(end, holidays);
// Result: 4
```

## DateTimeOffset Extensions

### Floor

Rounds a DateTimeOffset down to the nearest interval.

**Signature:**
```csharp
public static DateTimeOffset Floor(this DateTimeOffset dto, TimeSpan rounding)
```

**Parameters:**
- `dto`: The DateTimeOffset to round
- `rounding`: The interval to round to

**Returns:** DateTimeOffset rounded down to the nearest interval

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, TimeSpan.Zero);

// Round down to nearest hour
var hourFloor = timestamp.Floor(TimeSpan.FromHours(1));
// Result: 2024-01-15 14:00:00

// Round down to nearest 15 minutes
var minuteFloor = timestamp.Floor(TimeSpan.FromMinutes(15));
// Result: 2024-01-15 14:30:00
```

### Round

Rounds a DateTimeOffset to the nearest interval.

**Signature:**
```csharp
public static DateTimeOffset Round(this DateTimeOffset dto, TimeSpan rounding)
```

**Parameters:**
- `dto`: The DateTimeOffset to round
- `rounding`: The interval to round to

**Returns:** DateTimeOffset rounded to the nearest interval

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, TimeSpan.Zero);

// Round to nearest hour
var hourRound = timestamp.Round(TimeSpan.FromHours(1));
// Result: 2024-01-15 15:00:00 (rounds up from 14:37)

// Round to nearest 15 minutes
var minuteRound = timestamp.Round(TimeSpan.FromMinutes(15));
// Result: 2024-01-15 14:37:30 rounds to 14:45:00
```

### Ceiling

Rounds a DateTimeOffset up to the nearest interval.

**Signature:**
```csharp
public static DateTimeOffset Ceiling(this DateTimeOffset dto, TimeSpan rounding)
```

**Parameters:**
- `dto`: The DateTimeOffset to round
- `rounding`: The interval to round to

**Returns:** DateTimeOffset rounded up to the nearest interval

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, TimeSpan.Zero);

// Round up to nearest hour
var hourCeiling = timestamp.Ceiling(TimeSpan.FromHours(1));
// Result: 2024-01-15 15:00:00

// Round up to nearest 5 minutes
var minuteCeiling = timestamp.Ceiling(TimeSpan.FromMinutes(5));
// Result: 2024-01-15 14:40:00
```

### ToZeroTime

Converts a DateTimeOffset to midnight UTC (zero time).

**Signature:**
```csharp
public static DateTimeOffset ToZeroTime(this DateTimeOffset timestamp)
```

**Parameters:**
- `timestamp`: The timestamp to convert

**Returns:** DateTimeOffset at midnight UTC for the same date

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, TimeSpan.Zero);
var midnight = timestamp.ToZeroTime();
// Result: 2024-01-15 00:00:00 +00:00
```

### PerMinute

Rounds a DateTimeOffset to the minute level (sets seconds and milliseconds to zero).

**Signature:**
```csharp
public static DateTimeOffset PerMinute(this DateTimeOffset timestamp)
```

**Parameters:**
- `timestamp`: The timestamp to round

**Returns:** DateTimeOffset rounded to the minute level

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, 123, TimeSpan.Zero);
var perMinute = timestamp.PerMinute();
// Result: 2024-01-15 14:37:00 +00:00
```

### PerHour

Rounds a DateTimeOffset to the hour level (sets minutes, seconds, and milliseconds to zero).

**Signature:**
```csharp
public static DateTimeOffset PerHour(this DateTimeOffset timestamp)
```

**Parameters:**
- `timestamp`: The timestamp to round

**Returns:** DateTimeOffset rounded to the hour level

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, TimeSpan.Zero);
var perHour = timestamp.PerHour();
// Result: 2024-01-15 14:00:00 +00:00
```

### PerDay

Rounds a DateTimeOffset to the day level (sets time to midnight).

**Signature:**
```csharp
public static DateTimeOffset PerDay(this DateTimeOffset timestamp)
```

**Parameters:**
- `timestamp`: The timestamp to round

**Returns:** DateTimeOffset rounded to the day level (midnight)

**Example:**
```csharp
var timestamp = new DateTimeOffset(2024, 1, 15, 14, 37, 42, TimeSpan.Zero);
var perDay = timestamp.PerDay();
// Result: 2024-01-15 00:00:00 +00:00
```

## Common Use Cases

### Time Aggregation and Bucketing
```csharp
// Group timestamps by hour for analytics
var timestamp = DateTimeOffset.UtcNow;
var hourBucket = timestamp.PerHour();

// Use as dictionary key or group by clause
var eventsByHour = events
    .GroupBy(e => e.Timestamp.PerHour())
    .ToList();
```

### Data Retention and Archiving
```csharp
// Calculate daily boundaries for data partitioning
var today = DateTimeOffset.UtcNow.PerDay();
var yesterday = today.AddDays(-1);

// Query data for specific day
var dailyRecords = records
    .Where(r => r.Timestamp >= yesterday && r.Timestamp < today);
```

### Business Day Calculations
```csharp
// Calculate project duration in working days
var projectStart = new DateTime(2024, 1, 1);
var projectEnd = new DateTime(2024, 12, 31);

var holidays = GetCompanyHolidays(2024);
int workingDays = projectStart.GetWorkingDays(projectEnd, holidays);

// Calculate deadline
DateTime deadline = CalculateDeadline(DateTime.Today, workingDays: 10);
```

### Timezone Conversions for Global Applications
```csharp
// Convert UTC to user's timezone
var utcTime = DateTimeOffset.UtcNow;
var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
var localTime = utcTime.DateTime.GetDateTime(userTimeZone);

// Display in user's local time
Console.WriteLine($"Event time: {localTime:g}");
```

### Time Series Data Normalization
```csharp
// Normalize timestamps to 5-minute intervals for charting
var measurements = GetSensorData();
var normalizedData = measurements
    .Select(m => new
    {
        Bucket = m.Timestamp.Floor(TimeSpan.FromMinutes(5)),
        Value = m.Value
    })
    .GroupBy(x => x.Bucket)
    .Select(g => new
    {
        Time = g.Key,
        Average = g.Average(x => x.Value)
    });
```

## Tips and Best Practices

1. **Rounding Operations**: Use `Floor`, `Round`, and `Ceiling` for time-series bucketing and aggregation. These are more performant than calculating intervals manually.

2. **Working Days**: The `GetWorkingDays` method uses Saturday and Sunday as weekends. For different week structures (e.g., Friday-Saturday weekends), create a custom implementation.

3. **Timezone Awareness**:
   - Always use `DateTimeOffset` for timezone-aware operations
   - Store timestamps in UTC in databases
   - Convert to local timezone only for display

4. **PerX Methods**: Use `PerMinute`, `PerHour`, and `PerDay` for:
   - Database grouping operations
   - Cache key generation
   - Time-series data bucketing
   - Analytics aggregation

5. **Performance**: The rounding methods (`Floor`, `Round`, `Ceiling`) use tick-based arithmetic for optimal performance.

6. **Precision**: All `DateTimeOffset` rounding operations preserve the offset. Use `ToZeroTime()` to normalize to UTC midnight.

7. **Holiday Calendars**: When using `GetWorkingDays`, maintain a centralized holiday calendar per region/country for consistency across your application.
