namespace Stravaig.MonotonicClock.Tests;

public class MonotonicTimeProviderConcurrencyTests
{
    private static readonly DateTimeOffset BaseTime = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    private static readonly int ThreadCount = Math.Max(4, Environment.ProcessorCount);

    [Fact]
    public void GetUtcNow_OverTheSystemClock_IsStrictlyForwardOnlyAcrossThreads()
    {
        const int iterationsPerThread = 25_000;
        var provider = new MonotonicTimeProvider();

        long[][] perThread = RunConcurrently(provider, iterationsPerThread);

        AssertEachThreadSawAscendingTimes(perThread);
        AssertGloballyStrictlyIncreasing(perThread, provider.ResolutionTicks);
    }

    [Fact]
    public void GetUtcNow_WhenTheUnderlyingClockIsFrozen_ProducesOneUnbrokenSequenceAcrossThreads()
    {
        const int iterationsPerThread = 5_000;
        const long resolutionTicks = 1_000L;

        var provider = new MonotonicTimeProvider(resolutionTicks, new StubTimeProvider(BaseTime));

        long[][] perThread = RunConcurrently(provider, iterationsPerThread);

        AssertEachThreadSawAscendingTimes(perThread);

        // Nothing can be read from the frozen clock, so every value is synthesised: the
        // whole set, however it was interleaved, must be exactly one arithmetic sequence
        // of the configured resolution with no value issued twice.
        long[] all = perThread.SelectMany(t => t).OrderBy(t => t).ToArray();
        Assert.Equal(ThreadCount * iterationsPerThread, all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            Assert.Equal(BaseTime.UtcTicks + (i * resolutionTicks), all[i]);
        }
    }

    [Fact]
    public void GetUtcNow_OverTheSystemClock_AppliesTheResolutionAcrossThreads()
    {
        const int iterationsPerThread = 500;
        const long resolutionTicks = TimeSpan.TicksPerMillisecond;

        var provider = new MonotonicTimeProvider(resolutionTicks);

        long[][] perThread = RunConcurrently(provider, iterationsPerThread);

        AssertEachThreadSawAscendingTimes(perThread);
        AssertGloballyStrictlyIncreasing(perThread, resolutionTicks);
    }

    [Fact]
    public void GetUtcNow_ReadsTheUnderlyingClockOncePerCallAcrossThreads()
    {
        const int iterationsPerThread = 2_000;
        var stub = new StubTimeProvider(BaseTime);
        var provider = new MonotonicTimeProvider(1L, stub);

        RunConcurrently(provider, iterationsPerThread);

        Assert.Equal(ThreadCount * iterationsPerThread, stub.CallCount);
    }

    private static long[][] RunConcurrently(MonotonicTimeProvider provider, int iterationsPerThread)
    {
        var results = new long[ThreadCount][];
        using var barrier = new Barrier(ThreadCount);

        var threads = new Thread[ThreadCount];
        for (int t = 0; t < ThreadCount; t++)
        {
            int threadIndex = t;
            var times = new long[iterationsPerThread];
            results[threadIndex] = times;

            threads[threadIndex] = new Thread(() =>
            {
                // Start every thread together so the calls genuinely contend.
                barrier.SignalAndWait();
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    times[i] = provider.GetUtcNow().UtcTicks;
                }
            })
            {
                IsBackground = true,
                Name = $"monotonic-clock-{threadIndex}",
            };

            threads[threadIndex].Start();
        }

        foreach (var thread in threads)
        {
            Assert.True(thread.Join(TimeSpan.FromMinutes(1)), "A worker thread did not complete in time.");
        }

        return results;
    }

    private static void AssertEachThreadSawAscendingTimes(long[][] perThread)
    {
        for (int t = 0; t < perThread.Length; t++)
        {
            long[] times = perThread[t];
            for (int i = 1; i < times.Length; i++)
            {
                Assert.True(
                    times[i] > times[i - 1],
                    $"Thread {t} saw {times[i]} at index {i}, which is not later than {times[i - 1]} at index {i - 1}.");
            }
        }
    }

    private static void AssertGloballyStrictlyIncreasing(long[][] perThread, long resolutionTicks)
    {
        long[] all = perThread.SelectMany(t => t).OrderBy(t => t).ToArray();

        for (int i = 1; i < all.Length; i++)
        {
            long gap = all[i] - all[i - 1];
            Assert.True(
                gap >= resolutionTicks,
                $"Consecutive timestamps {all[i - 1]} and {all[i]} are {gap} ticks apart, which is less than the {resolutionTicks} tick resolution.");
        }

        Assert.Equal(all.Length, all.Distinct().Count());
    }
}
