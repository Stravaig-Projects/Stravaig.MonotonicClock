using System;
using System.Threading;

namespace Stravaig.MonotonicClock;

/// <summary>
/// A <see cref="TimeProvider"/> whose UTC timestamps only ever move forward.
/// </summary>
/// <remarks>
/// <para>
/// The underlying clock cannot be relied upon to produce an ascending sequence of
/// timestamps. It may be adjusted by the user or by NTP, it may drift in a virtualised
/// environment, and it very often has a resolution far coarser than a single tick, so
/// consecutive reads can return the same value or, occasionally, an earlier one.
/// </para>
/// <para>
/// Each call therefore returns the greater of the underlying clock's current value and
/// the previously issued value advanced by the configured resolution. When the underlying
/// clock has genuinely moved on, the real reading is used; when it has stalled or slipped
/// backwards, the value is synthesised so that the sequence remains strictly increasing.
/// The trade off is that a timestamp is not always a faithful reading of the underlying
/// clock, but it is always a strictly increasing ordering key that closely tracks it.
/// </para>
/// <para>
/// Instances are safe for concurrent use, and the guarantee holds across all threads
/// using the same instance. It does not extend across separate instances.
/// </para>
/// <para>
/// <see cref="TimeProvider.GetLocalNow"/> is derived from <see cref="GetUtcNow"/> and so
/// inherits the guarantee only as far as the local time zone allows: local time still goes
/// backwards where a time zone's offset does, such as at the end of daylight saving time.
/// </para>
/// </remarks>
public class MonotonicTimeProvider : TimeProvider
{
    /// <summary>
    /// The resolution, in ticks, used when no resolution is supplied. A single tick, which
    /// is the finest movement a <see cref="DateTimeOffset"/> can represent.
    /// </summary>
    private const long DefaultResolutionTicks = 1L;

    private static readonly long _maxTicks = DateTimeOffset.MaxValue.UtcTicks;

    private readonly TimeProvider _innerTimeProvider;
    private readonly long _resolutionTicks;
    private readonly object _syncLock = new();

    private long _lastUtcTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonotonicTimeProvider"/> class over the
    /// system clock, moving forward by <see cref="DefaultResolutionTicks"/> when the system
    /// clock does not move forward by at least that much.
    /// </summary>
    public MonotonicTimeProvider()
        : this(DefaultResolutionTicks, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonotonicTimeProvider"/> class over the
    /// system clock.
    /// </summary>
    /// <param name="resolutionTicks">
    /// The number of ticks the clock must move forward by when the system clock does not
    /// move forward by at least that much. Must be one or greater.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="resolutionTicks"/> is less than one.
    /// </exception>
    public MonotonicTimeProvider(long resolutionTicks)
        : this(resolutionTicks, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonotonicTimeProvider"/> class over the
    /// system clock.
    /// </summary>
    /// <param name="resolution">
    /// The interval the clock must move forward by when the system clock does not move
    /// forward by at least that much. Must be at least one tick.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="resolution"/> is less than one tick.
    /// </exception>
    public MonotonicTimeProvider(TimeSpan resolution)
        : this(resolution.Ticks, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonotonicTimeProvider"/> class over the
    /// supplied time provider.
    /// </summary>
    /// <param name="resolutionTicks">
    /// The number of ticks the clock must move forward by when
    /// <paramref name="innerTimeProvider"/> does not move forward by at least that much.
    /// Must be one or greater.
    /// </param>
    /// <param name="innerTimeProvider">The time provider to read the underlying time from.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="resolutionTicks"/> is less than one.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="innerTimeProvider"/> is <see langword="null"/>.
    /// </exception>
    public MonotonicTimeProvider(long resolutionTicks, TimeProvider innerTimeProvider)
    {
        if (resolutionTicks < 1L)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resolutionTicks),
                resolutionTicks,
                "The resolution must be at least one tick.");
        }

        ArgumentNullException.ThrowIfNull(innerTimeProvider);

        _resolutionTicks = resolutionTicks;
        _innerTimeProvider = innerTimeProvider;
    }

    /// <summary>
    /// Gets the number of ticks the clock moves forward by when the underlying time provider
    /// does not move forward by at least that much.
    /// </summary>
    public long ResolutionTicks => _resolutionTicks;

    /// <summary>
    /// Gets the interval the clock moves forward by when the underlying time provider does
    /// not move forward by at least that much.
    /// </summary>
    public TimeSpan Resolution => new TimeSpan(_resolutionTicks);

    /// <inheritdoc/>
    public override TimeZoneInfo LocalTimeZone => _innerTimeProvider.LocalTimeZone;

    /// <inheritdoc/>
    public override long TimestampFrequency => _innerTimeProvider.TimestampFrequency;

    /// <summary>
    /// Gets the current UTC date and time, guaranteed to be later than every value
    /// previously returned by this instance.
    /// </summary>
    /// <returns>
    /// The underlying time provider's current UTC time, or the previously returned value
    /// advanced by <see cref="ResolutionTicks"/>, whichever is the later.
    /// </returns>
    public override DateTimeOffset GetUtcNow()
    {
        long ticks;
        lock (_syncLock)
        {
            long nowTicks = _innerTimeProvider.GetUtcNow().UtcTicks;

            // Saturate rather than overflow. Only reachable at the very end of the
            // representable range, where there is nowhere left to move forward to.
            long earliestPermittedTicks = _lastUtcTicks <= _maxTicks - _resolutionTicks
                ? _lastUtcTicks + _resolutionTicks
                : _maxTicks;

            ticks = Math.Max(earliestPermittedTicks, nowTicks);
            _lastUtcTicks = ticks;
        }

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    /// <inheritdoc/>
    public override long GetTimestamp() => _innerTimeProvider.GetTimestamp();

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => _innerTimeProvider.CreateTimer(callback, state, dueTime, period);
}
