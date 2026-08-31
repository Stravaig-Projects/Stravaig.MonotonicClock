using System;

namespace Stravaig.MonotonicClock;

/// <summary>
/// Provides a monotonic clock that ensures time values are guaranteed to increase monotonically.
/// </summary>
public static class MonotonicClock
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    public static DateTime Now => MonotonicClockOffset.Now.DateTime;

    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    public static DateTime UtcNow => MonotonicClockOffset.UtcNow.DateTime;
}
