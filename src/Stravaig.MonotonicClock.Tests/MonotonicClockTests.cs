using Shouldly;

namespace Stravaig.MonotonicClock.Tests;

public class MonotonicClockTests
{
    [Fact]
    public void UtcNow_HasUtcKind()
    {
        MonotonicClock.UtcNow.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Now_HasLocalKind()
    {
        MonotonicClock.Now.Kind.ShouldBe(DateTimeKind.Local);
    }

    [Fact]
    public void UtcNow_IsCloseToTheSystemUtcTime()
    {
        var before = DateTime.UtcNow;
        var actual = MonotonicClock.UtcNow;
        var after = DateTime.UtcNow;

        actual.ShouldBeInRange(
            before.AddSeconds(-1),
            after.AddSeconds(1));
    }

    [Fact]
    public void Now_IsCloseToTheSystemLocalTime()
    {
        var before = DateTime.Now;
        var actual = MonotonicClock.Now;
        var after = DateTime.Now;

        actual.ShouldBeInRange(
            before.AddSeconds(-1),
            after.AddSeconds(1));
    }

    [Fact]
    public void UtcNow_RoundTripsThroughDateTimeOffsetWithoutShiftingTheInstant()
    {
        var utcNow = MonotonicClock.UtcNow;

        // A Utc-kinded DateTime is treated as an absolute instant, so wrapping it back
        // into a DateTimeOffset must leave the instant unchanged.
        new DateTimeOffset(utcNow).UtcDateTime.ShouldBe(utcNow);
    }

    [Fact]
    public void UtcNow_IsStrictlyAscendingAcrossManyReads()
    {
        const int iterations = 1_000;
        var times = new DateTime[iterations];
        for (int i = 0; i < iterations; i++)
        {
            times[i] = MonotonicClock.UtcNow;
        }

        for (int i = 1; i < iterations; i++)
        {
            times[i].Kind.ShouldBe(DateTimeKind.Utc);
            times[i].ShouldBeGreaterThan(times[i - 1]);
        }
    }

    [Fact]
    public void Now_IsStrictlyAscendingAcrossManyReads()
    {
        const int iterations = 1_000;
        var times = new DateTime[iterations];
        for (int i = 0; i < iterations; i++)
        {
            times[i] = MonotonicClock.Now;
        }

        for (int i = 1; i < iterations; i++)
        {
            times[i].Kind.ShouldBe(DateTimeKind.Local);
            times[i].ShouldBeGreaterThan(times[i - 1]);
        }
    }
}
