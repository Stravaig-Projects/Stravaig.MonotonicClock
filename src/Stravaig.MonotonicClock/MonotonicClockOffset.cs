using System;

namespace Stravaig.MonotonicClock;

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
