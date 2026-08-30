namespace Stravaig.MonotonicClock.Tests;

/// <summary>
/// A time provider whose UTC time is entirely under the control of the test, so that a
/// stalled, backward stepping, or jumping underlying clock can be reproduced on demand.
/// </summary>
internal sealed class StubTimeProvider : TimeProvider
{
    private readonly Func<int, DateTimeOffset> _sequence;
    private int _callCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubTimeProvider"/> class that always
    /// reports the same time, as a clock with a coarse resolution appears to do.
    /// </summary>
    /// <param name="frozenAt">The time every read reports.</param>
    public StubTimeProvider(DateTimeOffset frozenAt)
        : this(_ => frozenAt)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StubTimeProvider"/> class whose reads
    /// are produced from the zero based index of the read.
    /// </summary>
    /// <param name="sequence">Produces the time for a given read index.</param>
    public StubTimeProvider(Func<int, DateTimeOffset> sequence)
    {
        _sequence = sequence;
    }

    /// <summary>Gets the number of times the UTC time has been read.</summary>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow()
    {
        int index = Interlocked.Increment(ref _callCount) - 1;
        return _sequence(index);
    }
}
