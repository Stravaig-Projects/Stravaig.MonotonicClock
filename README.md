# Stravaig Monotonic Clock

A monotonic clock that ensures strict forward only capture of timestamps. This project is based on this [blog post about out of sequence timestamps](https://colinangusmackay.github.io/2025/02/25/fixing-out-of-sequence-timestamps/). The fix is essentially extracted out into this project.

## Usage

For simple use there is a `MonotonicClock` class with some basic methods to get the current time.

```csharp
public static class MonotonicClock
{
    public static DateTime Now { get; }
    
    public static DateTime UtcNow { get; }
    
    public static DateTimeOffset NowOffset { get; }
    
    public static DateTimeOffset UtcNowOffset { get; } 
}
```

There is also a `MonotonicTimeProvider` class, which is an implementation of the `TimeProvider` that overrides the base class implementations to ensure that all times are strictly forward only. This can then be used in any code that uses the `TimeProvider` interface. It also allows the user to configure the resolution of the forward movement. (The default is 1 tick.)

## Contributing / Getting Started

* Ensure you have PowerShell 7.1.x or higher installed
* At a PowerShell prompt
    * Navigate to the root of this repository
    * Run `./Install-GitHooks.ps1`
* Build and test from the repository root with `dotnet test src/Stravaig.MonotonicClock.sln`
