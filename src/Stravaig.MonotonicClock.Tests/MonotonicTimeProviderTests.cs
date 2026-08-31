using Shouldly;

namespace Stravaig.MonotonicClock.Tests;

public class MonotonicTimeProviderTests
{
    private static readonly DateTimeOffset BaseTime = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Constructor_WithResolutionOfLessThanOneTick_Throws(long resolutionTicks)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => new MonotonicTimeProvider(resolutionTicks));

        exception.ParamName.ShouldBe("resolutionTicks");
    }

    [Fact]
    public void Constructor_WithResolutionOfLessThanOneTickAsTimeSpan_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new MonotonicTimeProvider(TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_WithNullInnerTimeProvider_Throws()
    {
        var exception = Should.Throw<ArgumentNullException>(
            () => new MonotonicTimeProvider(1L, null!));

        exception.ParamName.ShouldBe("innerTimeProvider");
    }

    [Fact]
    public void Constructor_WithNoResolution_UsesASingleTick()
    {
        var provider = new MonotonicTimeProvider();

        provider.ResolutionTicks.ShouldBe(1L);
        provider.Resolution.ShouldBe(TimeSpan.FromTicks(1L));
    }

    [Fact]
    public void Constructor_WithTimeSpanResolution_ExposesTheEquivalentTicks()
    {
        var provider = new MonotonicTimeProvider(TimeSpan.FromMilliseconds(5));

        provider.ResolutionTicks.ShouldBe(5L * TimeSpan.TicksPerMillisecond);
        provider.Resolution.ShouldBe(TimeSpan.FromMilliseconds(5));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(5000L)]
    [InlineData(TimeSpan.TicksPerSecond)]
    public void GetUtcNow_OnTheFirstCall_ReturnsTheUnderlyingTimeUnaltered(long resolutionTicks)
    {
        var provider = new MonotonicTimeProvider(resolutionTicks, new StubTimeProvider(BaseTime));

        var actual = provider.GetUtcNow();

        actual.UtcTicks.ShouldBe(BaseTime.UtcTicks);
    }

    [Fact]
    public void GetUtcNow_ReturnsValuesWithAZeroOffset()
    {
        var provider = new MonotonicTimeProvider(
            1L,
            new StubTimeProvider(BaseTime.ToOffset(TimeSpan.FromHours(5))));

        var actual = provider.GetUtcNow();

        actual.Offset.ShouldBe(TimeSpan.Zero);
        actual.UtcTicks.ShouldBe(BaseTime.UtcTicks);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(2L)]
    [InlineData(17L)]
    [InlineData(1000L)]
    [InlineData(TimeSpan.TicksPerMillisecond)]
    public void GetUtcNow_WhenTheUnderlyingClockIsFrozen_AdvancesByExactlyTheResolution(long resolutionTicks)
    {
        const int iterations = 100;
        var provider = new MonotonicTimeProvider(resolutionTicks, new StubTimeProvider(BaseTime));

        var actual = new long[iterations];
        for (int i = 0; i < iterations; i++)
        {
            actual[i] = provider.GetUtcNow().UtcTicks;
        }

        for (int i = 0; i < iterations; i++)
        {
            actual[i].ShouldBe(BaseTime.UtcTicks + (i * resolutionTicks));
        }
    }

    [Fact]
    public void GetUtcNow_WhenTheUnderlyingClockMovesForwardByLessThanTheResolution_AdvancesByTheResolution()
    {
        const long resolutionTicks = 1000L;
        var stub = new StubTimeProvider(index => BaseTime.AddTicks(index * 500L));
        var provider = new MonotonicTimeProvider(resolutionTicks, stub);

        long first = provider.GetUtcNow().UtcTicks;
        long second = provider.GetUtcNow().UtcTicks;
        long third = provider.GetUtcNow().UtcTicks;

        first.ShouldBe(BaseTime.UtcTicks);
        second.ShouldBe(BaseTime.UtcTicks + resolutionTicks);
        third.ShouldBe(BaseTime.UtcTicks + (2 * resolutionTicks));
    }

    [Fact]
    public void GetUtcNow_WhenTheUnderlyingClockMovesForwardByMoreThanTheResolution_ReturnsTheUnderlyingTime()
    {
        const long resolutionTicks = 1000L;
        var stub = new StubTimeProvider(index => BaseTime.AddTicks(index * 25_000L));
        var provider = new MonotonicTimeProvider(resolutionTicks, stub);

        long first = provider.GetUtcNow().UtcTicks;
        long second = provider.GetUtcNow().UtcTicks;
        long third = provider.GetUtcNow().UtcTicks;

        first.ShouldBe(BaseTime.UtcTicks);
        second.ShouldBe(BaseTime.UtcTicks + 25_000L);
        third.ShouldBe(BaseTime.UtcTicks + 50_000L);
    }

    [Fact]
    public void GetUtcNow_WhenTheUnderlyingClockGoesBackwards_StillAdvancesByTheResolution()
    {
        const long resolutionTicks = 5L;
        const int iterations = 50;

        // A clock being wound back, whether by the user, by NTP, or by drift in a
        // virtualised environment.
        var stub = new StubTimeProvider(index => BaseTime.AddTicks(-index * 1000L));
        var provider = new MonotonicTimeProvider(resolutionTicks, stub);

        for (int i = 0; i < iterations; i++)
        {
            long actual = provider.GetUtcNow().UtcTicks;
            actual.ShouldBe(BaseTime.UtcTicks + (i * resolutionTicks));
        }
    }

    [Fact]
    public void GetUtcNow_WhenTheUnderlyingClockCatchesUp_ReturnsTheUnderlyingTimeAgain()
    {
        const long resolutionTicks = 1000L;
        long[] offsets = [0L, 0L, 0L, 2_500L, 10_000L];
        var stub = new StubTimeProvider(index => BaseTime.AddTicks(offsets[index]));
        var provider = new MonotonicTimeProvider(resolutionTicks, stub);

        var actual = new long[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            actual[i] = provider.GetUtcNow().UtcTicks;
        }

        actual[0].ShouldBe(BaseTime.UtcTicks);
        actual[1].ShouldBe(BaseTime.UtcTicks + 1_000L);
        actual[2].ShouldBe(BaseTime.UtcTicks + 2_000L);

        // The underlying clock has moved, but not past the last issued value, so the
        // synthesised value still wins.
        actual[3].ShouldBe(BaseTime.UtcTicks + 3_000L);

        // Now it has moved past, so the real reading is used and nothing is synthesised.
        actual[4].ShouldBe(BaseTime.UtcTicks + 10_000L);
    }

    [Fact]
    public void GetUtcNow_ReadsTheUnderlyingClockOncePerCall()
    {
        const int iterations = 100;
        var stub = new StubTimeProvider(BaseTime);
        var provider = new MonotonicTimeProvider(1L, stub);

        for (int i = 0; i < iterations; i++)
        {
            provider.GetUtcNow();
        }

        stub.CallCount.ShouldBe(iterations);
    }

    [Fact]
    public void GetUtcNow_OverTheSystemClock_IsStrictlyForwardOnly()
    {
        const int iterations = 250_000;
        var provider = new MonotonicTimeProvider();

        var times = new DateTimeOffset[iterations];
        for (int i = 0; i < iterations; i++)
        {
            times[i] = provider.GetUtcNow();
        }

        for (int i = 1; i < iterations; i++)
        {
            times[i].ShouldBeGreaterThan(
                times[i - 1],
                $"Time at index {i} ({times[i]:O}) is not later than the time at index {i - 1} ({times[i - 1]:O}).");
        }

        times.Distinct().Count().ShouldBe(iterations);
    }

    [Fact]
    public void GetLocalNow_IsStrictlyForwardOnlyWhenTheUnderlyingClockIsFrozen()
    {
        const int iterations = 100;
        var provider = new MonotonicTimeProvider(1L, new StubTimeProvider(BaseTime));

        var times = new DateTimeOffset[iterations];
        for (int i = 0; i < iterations; i++)
        {
            times[i] = provider.GetLocalNow();
        }

        for (int i = 1; i < iterations; i++)
        {
            times[i].ShouldBeGreaterThan(
                times[i - 1],
                $"Local time at index {i} is not later than at index {i - 1}.");
        }
    }

    [Fact]
    public void UnderlyingTimeProviderConcernsAreDelegated()
    {
        var provider = new MonotonicTimeProvider(1L, TimeProvider.System);

        provider.LocalTimeZone.ShouldBe(TimeProvider.System.LocalTimeZone);
        provider.TimestampFrequency.ShouldBe(TimeProvider.System.TimestampFrequency);

        long first = provider.GetTimestamp();
        long second = provider.GetTimestamp();
        second.ShouldBeGreaterThanOrEqualTo(first);
    }

    [Fact]
    public void SeparateInstancesKeepSeparateState()
    {
        var first = new MonotonicTimeProvider(1_000L, new StubTimeProvider(BaseTime));
        var second = new MonotonicTimeProvider(1_000L, new StubTimeProvider(BaseTime));

        first.GetUtcNow();
        first.GetUtcNow();

        second.GetUtcNow().UtcTicks.ShouldBe(BaseTime.UtcTicks);
    }

    [Fact]
    public void ManyIterationsAreAscendingOnly()
    {
        const int iterations = 10_000_000;
        DateTimeOffset[] times = new DateTimeOffset[iterations];
        var provider = new MonotonicTimeProvider();
        for (int i = 0; i < iterations; i++)
        {
            times[i] = provider.GetUtcNow();
        }

        for (int i = 1; i < iterations; i++)
        {
            times[i].ShouldBeGreaterThan(times[i - 1]);
        }
    }
}
