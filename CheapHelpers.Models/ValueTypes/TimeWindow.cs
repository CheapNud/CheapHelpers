namespace CheapHelpers.Models.ValueTypes;

/// <summary>
/// Represents a time interval with a start and end point.
/// Useful for billing periods, scheduling windows, peak tracking, and any time-interval logic.
/// </summary>
public readonly record struct TimeWindow(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>
    /// Gets the duration of this time window.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Checks whether this time window contains the specified point in time.
    /// </summary>
    public bool Contains(DateTimeOffset point) => point >= Start && point < End;

    /// <summary>
    /// Checks whether this time window overlaps with another time window.
    /// </summary>
    public bool Overlaps(TimeWindow other) => Start < other.End && other.Start < End;

    /// <summary>
    /// Returns the intersection of this time window with another, or null if they don't overlap.
    /// </summary>
    public TimeWindow? Intersect(TimeWindow other)
    {
        if (!Overlaps(other))
            return null;

        var intersectStart = Start > other.Start ? Start : other.Start;
        var intersectEnd = End < other.End ? End : other.End;
        return new TimeWindow(intersectStart, intersectEnd);
    }

    /// <summary>
    /// Creates a time window starting at the current moment with the specified duration.
    /// </summary>
    public static TimeWindow Current(TimeSpan interval)
    {
        var now = DateTimeOffset.UtcNow;
        long ticks = now.Ticks / interval.Ticks;
        var windowStart = new DateTimeOffset(ticks * interval.Ticks, TimeSpan.Zero);
        return new TimeWindow(windowStart, windowStart + interval);
    }

    /// <summary>
    /// Creates a time window for a specific interval aligned to the given base time.
    /// </summary>
    public static TimeWindow ForInterval(DateTimeOffset baseTime, TimeSpan interval)
    {
        long ticks = baseTime.Ticks / interval.Ticks;
        var windowStart = new DateTimeOffset(ticks * interval.Ticks, TimeSpan.Zero);
        return new TimeWindow(windowStart, windowStart + interval);
    }

    /// <summary>
    /// Creates a sequence of consecutive time windows spanning the given range.
    /// </summary>
    public static IEnumerable<TimeWindow> Enumerate(DateTimeOffset from, DateTimeOffset until, TimeSpan interval)
    {
        var current = ForInterval(from, interval);
        while (current.Start < until)
        {
            yield return current;
            current = new TimeWindow(current.End, current.End + interval);
        }
    }

    public override string ToString() => $"[{Start:O} → {End:O}] ({Duration})";
}
