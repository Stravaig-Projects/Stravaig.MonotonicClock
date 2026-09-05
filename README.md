# Stravaig Monotonic Clock

A monotonic clock that ensures strict forward only capture of timestamps. This project is based on this [blog post about out of sequence timestamps](https://colinangusmackay.github.io/2025/02/25/fixing-out-of-sequence-timestamps/). The fix is essentially extracted out into this project and into a subclass of [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) to make it easy to access the functionality.

## Why create this?

It turns out that occasionally a `DateTime` or `DateTimeOffset` are captured out of sequence. This can cause problems when using these values to order events. This project provides a monotonic clock that ensures that timestamps are always captured in sequence. 

There are a few reasons this may happen, such as threading issues or system clock adjustments.

So, what happens is that when the MonotonicTimeProvider calls DateTimeOffset.UtcNow, and the time appears to have gone backwards then it returns a value that is the same as the previous call incremented by the "resolution", which is one tick by default. The means that every call is always an increment from the last. So anything that relies on ordering 

### The Trade Off

If you assume that the system clock is always accurate, then this time provider may seem to add an element of inaccuracy. However, the system clock isn't necessarily accurate either. It can drift, and your OS will call a time-server occasionally to correct it. So, while the monotonic time provider will force the times returned to be ever-increasing, the rate it does this is so small that a typical correction, which may be in the realm of milliseconds to seconds, then the two clocks will reconverge soon.

## Usage

Add the nuget package [Stravaig.MonotonicClock](https://www.nuget.org/packages/Stravaig.MonotonicClock) to your project.

### Simple Usage

For simple use there are the `MonotonicClock` and `MonotonicClockOffset` classes with some basic methods to get the current time.
This is a façade to the default instance of the `MonotonicTimeProvider` class (see below).

```csharp
public static class MonotonicClock
{
    public static DateTime Now { get; }
    
    public static DateTime UtcNow { get; }
}

public static class MonotonicClockOffset
{
    public static DateTimeOffset NowOffset { get; }
    
    public static DateTimeOffset UtcNowOffset { get; } 
}
```

### Via the MonotonicTimeProvider

There is also a `MonotonicTimeProvider` class, which is an implementation of the `TimeProvider` that overrides the base class implementations to ensure that all times are strictly forward only. This can then be used in any code that uses the `TimeProvider` interface. It also allows the user to configure the resolution of the forward movement. (The default is one tick.)

In order not to bring in any unnecessary dependencies, there is a separate Stravaig.MonotonicClock.DependencyInjection package that wraps up the helper methods for registering the `MonotonicTimeProvider` with Microsoft's dependency injection library.

```csharp
// Add the time provider if one is not already registered.
services.TryAddMonotonicClock();

// Forcefully add the time provider (Will replace any existing TimeProvider)
services.AddMonotonicClock();

// Add the time provider as a keyed service.
services.AddKeyedMonotonicClock(key);
```

## Extended use

The default instance, accessible via `MonotonicTimeProvider.Instance`, is configured to ensure that the number of ticks
increments by at least one each time `GetLocalNow()` or `GetUtcNow()` is called.

You can set the resolution of the `MonotonicTimeProvider` by passing in a `TimeSpan` or `long` representing the minimum 
number of ticks the clock moves forward by between successive calls. It is recommended to keep this number low so that
the clock does not skew into the future.

e.g. To set the resolution to 1 microsecond:
```csharp
var timeProvider = new MonotonicTimeProvider(TimeSpan.FromMicroseconds(1));
// or
var timeProvider = new MonotonicTimeProvider(TimeSpan.TicksPerMicrosecond);
```

## Contributing / Getting Started

* Ensure you have PowerShell 7.1.x or higher installed
* At a PowerShell prompt
    * Navigate to the root of this repository
    * Run `./Install-GitHooks.ps1`
* Build and test from the repository root with `dotnet test src/Stravaig.MonotonicClock.sln`
