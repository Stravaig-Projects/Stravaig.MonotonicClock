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
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MonotonicTimeProvider(resolutionTicks));

        Assert.Equal("resolutionTicks", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithResolutionOfLessThanOneTickAsTimeSpan_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MonotonicTimeProvider(TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_WithNullInnerTimeProvider_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new MonotonicTimeProvider(1L, null!));

        Assert.Equal("innerTimeProvider", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNoResolution_UsesASingleTick()
    {
        var provider = new MonotonicTimeProvider();

        Assert.Equal(1L, provider.ResolutionTicks);
        Assert.Equal(TimeSpan.FromTicks(1L), provider.Resolution);
    }

    [Fact]
    public void Constructor_WithTimeSpanResolution_ExposesTheEquivalentTicks()
    {
        var provider = new MonotonicTimeProvider(TimeSpan.FromMilliseconds(5));

        Assert.Equal(5L * TimeSpan.TicksPerMillisecond, provider.ResolutionTicks);
        Assert.Equal(TimeSpan.FromMilliseconds(5), provider.Resolution);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(5000L)]
    [InlineData(TimeSpan.TicksPerSecond)]
    public void GetUtcNow_OnTheFirstCall_ReturnsTheUnderlyingTimeUnaltered(long resolutionTicks)
    {
        var provider = new MonotonicTimeProvider(resolutionTicks, new StubTimeProvider(BaseTime));

        var actual = provider.GetUtcNow();

        Assert.Equal(BaseTime.UtcTicks, actual.UtcTicks);
    }

    [Fact]
    public void GetUtcNow_ReturnsValuesWithAZeroOffset()
    {
        var provider = new MonotonicTimeProvider(
            1L,
            new StubTimeProvider(BaseTime.ToOffset(TimeSpan.FromHours(5))));

        var actual = provider.GetUtcNow();

        Assert.Equal(TimeSpan.Zero, actual.Offset);
        Assert.Equal(BaseTime.UtcTicks, actual.UtcTicks);
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
            Assert.Equal(BaseTime.UtcTicks + (i * resolutionTicks), actual[i]);
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

        Assert.Equal(BaseTime.UtcTicks, first);
        Assert.Equal(BaseTime.UtcTicks + resolutionTicks, second);
        Assert.Equal(BaseTime.UtcTicks + (2 * resolutionTicks), third);
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

        Assert.Equal(BaseTime.UtcTicks, first);
        Assert.Equal(BaseTime.UtcTicks + 25_000L, second);
        Assert.Equal(BaseTime.UtcTicks + 50_000L, third);
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
            Assert.Equal(BaseTime.UtcTicks + (i * resolutionTicks), actual);
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

        Assert.Equal(BaseTime.UtcTicks, actual[0]);
        Assert.Equal(BaseTime.UtcTicks + 1_000L, actual[1]);
        Assert.Equal(BaseTime.UtcTicks + 2_000L, actual[2]);

        // The underlying clock has moved, but not past the last issued value, so the
        // synthesised value still wins.
        Assert.Equal(BaseTime.UtcTicks + 3_000L, actual[3]);

        // Now it has moved past, so the real reading is used and nothing is synthesised.
        Assert.Equal(BaseTime.UtcTicks + 10_000L, actual[4]);
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

        Assert.Equal(iterations, stub.CallCount);
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
            Assert.True(
                times[i] > times[i - 1],
                $"Time at index {i} ({times[i]:O}) is not later than the time at index {i - 1} ({times[i - 1]:O}).");
        }

        Assert.Equal(iterations, times.Distinct().Count());
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
            Assert.True(times[i] > times[i - 1], $"Local time at index {i} is not later than at index {i - 1}.");
        }
    }

    [Fact]
    public void UnderlyingTimeProviderConcernsAreDelegated()
    {
        var provider = new MonotonicTimeProvider(1L, TimeProvider.System);

        Assert.Equal(TimeProvider.System.LocalTimeZone, provider.LocalTimeZone);
        Assert.Equal(TimeProvider.System.TimestampFrequency, provider.TimestampFrequency);

        long first = provider.GetTimestamp();
        long second = provider.GetTimestamp();
        Assert.True(second >= first);
    }

    [Fact]
    public void SeparateInstancesKeepSeparateState()
    {
        var first = new MonotonicTimeProvider(1_000L, new StubTimeProvider(BaseTime));
        var second = new MonotonicTimeProvider(1_000L, new StubTimeProvider(BaseTime));

        first.GetUtcNow();
        first.GetUtcNow();

        Assert.Equal(BaseTime.UtcTicks, second.GetUtcNow().UtcTicks);
    }
}
