using System.Globalization;

namespace CheapHelpers.MediaProcessing.Utilities;

/// <summary>
/// Utility methods for converting between video frame numbers and timecode strings.
/// </summary>
public static class TimecodeHelper
{
    /// <summary>
    /// Converts a frame number to a timecode string in HH:MM:SS.mmm format.
    /// </summary>
    /// <param name="frames">The frame number to convert.</param>
    /// <param name="frameRate">The video frame rate (frames per second).</param>
    /// <returns>A timecode string in HH:MM:SS.mmm format.</returns>
    /// <example>
    /// TimecodeHelper.FramesToTimecode(1800, 30.0) returns "00:01:00.000"
    /// TimecodeHelper.FramesToTimecode(5400, 24.0) returns "00:03:45.000"
    /// </example>
    public static string FramesToTimecode(int frames, double frameRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameRate);
        var totalSeconds = frames / frameRate;
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);

        return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
    }

    /// <summary>
    /// Converts a timecode string to a frame number.
    /// Supports HH:MM:SS and HH:MM:SS.mmm formats.
    /// </summary>
    /// <param name="timecode">The timecode string to parse.</param>
    /// <param name="frameRate">The video frame rate (frames per second).</param>
    /// <returns>The frame number corresponding to the timecode.</returns>
    /// <exception cref="FormatException">Thrown when the timecode format is not recognized.</exception>
    /// <example>
    /// TimecodeHelper.TimecodeToFrames("00:01:00.000", 30.0) returns 1800
    /// TimecodeHelper.TimecodeToFrames("01:30:00", 24.0) returns 129600
    /// </example>
    public static int TimecodeToFrames(string timecode, double frameRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameRate);
        var parts = timecode.Split(':');
        if (parts.Length != 3)
            throw new FormatException($"Timecode must be in HH:MM:SS or HH:MM:SS.mmm format. Got: '{timecode}'");

        var hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);

        var secondsParts = parts[2].Split('.');
        var seconds = int.Parse(secondsParts[0], CultureInfo.InvariantCulture);
        var milliseconds = secondsParts.Length > 1
            ? int.Parse(secondsParts[1].PadRight(3, '0')[..3], CultureInfo.InvariantCulture)
            : 0;

        var timeSpan = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        return (int)(timeSpan.TotalSeconds * frameRate);
    }
}
