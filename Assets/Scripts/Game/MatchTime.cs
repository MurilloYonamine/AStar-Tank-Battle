using System;
using System.Globalization;

/// <summary>
/// Uses the database's millisecond precision for both results and ranking ties.
/// This is elapsed combat time, never a conversion from seconds into score points.
/// </summary>
public static class MatchTime
{
    public static string Format(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            return "--:--,---";
        long milliseconds = (long)Math.Round(seconds * 1000d, MidpointRounding.AwayFromZero);
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00},{2:000}",
            milliseconds / 60000, milliseconds / 1000 % 60, milliseconds % 1000);
    }
}
