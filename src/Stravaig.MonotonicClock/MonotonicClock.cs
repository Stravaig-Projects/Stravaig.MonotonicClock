using System;

namespace Stravaig.MonotonicClock;

/// <summary>
/// Provides a monotonic clock that ensures time values are guaranteed to increase monotonically.
/// </summary>
public static class MonotonicClock
{
    /// <summary>
    /// Gets the current local date and time, with <see cref="DateTime.Kind"/> set to
    /// <see cref="DateTimeKind.Local"/>.
    /// </summary>
    public static DateTime Now => MonotonicClockOffset.Now.LocalDateTime;

    /// <summary>
    /// Gets the current UTC date and time, with <see cref="DateTime.Kind"/> set to
    /// <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    public static DateTime UtcNow => MonotonicClockOffset.UtcNow.UtcDateTime;
}
