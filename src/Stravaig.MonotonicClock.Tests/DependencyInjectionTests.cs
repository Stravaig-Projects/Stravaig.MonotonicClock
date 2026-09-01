using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Stravaig.MonotonicClock.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void EnsureThatTheMonotonicTimeProviderIsRegistered()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMonotonicClock();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

        // Assert
        timeProvider.ShouldBeOfType<MonotonicTimeProvider>();
    }

    [Fact]
    public void EnsureThatAddMonotonicClockAlwaysResolvesTheSameInstance()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMonotonicClock();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var resolution1 = serviceProvider.GetRequiredService<TimeProvider>();
        var resolution2 = serviceProvider.GetRequiredService<TimeProvider>();
        using var scope = serviceProvider.CreateScope();
        var resolution3 = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Assert
        resolution1.ShouldNotBeNull();
        resolution2.ShouldNotBeNull();
        resolution1.ShouldBeSameAs(resolution2);
        resolution3.ShouldNotBeNull();
        resolution3.ShouldBeSameAs(resolution1);
    }

    [Fact]
    public void EnsureThatTheKeyedMonotonicTimeProviderIsRegistered()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddKeyedMonotonicClock("my-key");
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var timeProvider = serviceProvider.GetRequiredKeyedService<TimeProvider>("my-key");
        var notATimeProvider = serviceProvider.GetKeyedService<TimeProvider>("other-key");

        // Assert
        timeProvider.ShouldBeOfType<MonotonicTimeProvider>();
        notATimeProvider.ShouldBeNull();
    }

    [Fact]
    public void EnsureThatAddKeyedMonotonicClockAlwaysResolvesTheSameInstance()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddKeyedMonotonicClock("my-key");
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var resolution1 = serviceProvider.GetRequiredKeyedService<TimeProvider>("my-key");
        var resolution2 = serviceProvider.GetRequiredKeyedService<TimeProvider>("my-key");
        using var scope = serviceProvider.CreateScope();
        var resolution3 = scope.ServiceProvider.GetRequiredKeyedService<TimeProvider>("my-key");

        // Assert
        resolution1.ShouldNotBeNull();
        resolution2.ShouldNotBeNull();
        resolution1.ShouldBeSameAs(resolution2);
        resolution3.ShouldNotBeNull();
        resolution3.ShouldBeSameAs(resolution1);
    }

    [Fact]
    public void EnsureThatATimeProviderIsNotResolvedUnlessAdded()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var timeProvider = serviceProvider.GetService<TimeProvider>();

        // Assert
        timeProvider.ShouldBeNull();
    }

    [Fact]
    public void EnsureThatTryAddDoesNotAddIfATimeProviderIsAlreadyRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(TimeProvider.System);
        serviceCollection.TryAddMonotonicClock();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var timeProvider = serviceProvider.GetService<TimeProvider>();

        // Assert
        timeProvider.ShouldNotBeNull();
        timeProvider.ShouldNotBeOfType<MonotonicTimeProvider>();
        timeProvider.GetType().Name.ShouldBe("SystemTimeProvider");
    }

    [Fact]
    public void EnsureThatTryAddMonotonicClockAlwaysResolvesTheSameInstance()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.TryAddMonotonicClock();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var resolution1 = serviceProvider.GetRequiredService<TimeProvider>();
        var resolution2 = serviceProvider.GetRequiredService<TimeProvider>();
        using var scope = serviceProvider.CreateScope();
        var resolution3 = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Assert
        resolution1.ShouldNotBeNull();
        resolution2.ShouldNotBeNull();
        resolution1.ShouldBeSameAs(resolution2);
        resolution3.ShouldNotBeNull();
        resolution3.ShouldBeSameAs(resolution1);
    }

    [Fact]
    public void EnsureThatTryAddAddsIfATimeProviderIsNotAlreadyRegistered()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.TryAddMonotonicClock();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var timeProvider = serviceProvider.GetService<TimeProvider>();

        // Assert
        timeProvider.ShouldNotBeNull();
        timeProvider.ShouldBeOfType<MonotonicTimeProvider>();
    }
}
