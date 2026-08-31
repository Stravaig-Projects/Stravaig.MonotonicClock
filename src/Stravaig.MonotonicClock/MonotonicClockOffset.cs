using System;

namespace Stravaig.MonotonicClock;

/// <summary>
/// A helper class for getting the DateTimeOffset from the monotonic clock.
/// </summary>
public static class MonotonicClockOffset
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    public static DateTimeOffset Now => MonotonicTimeProvider.Instance.GetLocalNow();

    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    public static DateTimeOffset UtcNow => MonotonicTimeProvider.Instance.GetUtcNow();
}
