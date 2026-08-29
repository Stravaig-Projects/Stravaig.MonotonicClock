---
layout: default
title: Stravaig Monotonic Clock API
---

# Stravaig Monotonic Clock API

`StopwatchMonotonicClock` provides a shared monotonic clock backed by `System.Diagnostics.Stopwatch`.

Use `GetTimestamp()` to capture the start of an operation and `GetElapsedTime(...)` to calculate elapsed time from those timestamps.
