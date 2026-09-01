using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// The convention is that the ServiceCollection extensions are in the same namespace as the service they extend.
// ReSharper disable once CheckNamespace
namespace Stravaig.MonotonicClock;

/// <summary>
/// Extension methods for the <see cref="IServiceCollection"/> to add the <see cref="MonotonicTimeProvider"/> to the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="MonotonicTimeProvider"/> the <see cref="IServiceCollection"/> if a <see cref="TimeProvider"/> has not already been added.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <see cref="IServiceCollection"/> to allow chained calls.</returns>
    public static IServiceCollection TryAddMonotonicClock(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(MonotonicTimeProvider.Instance);
        return services;
    }

    /// <summary>
    /// Adds the <see cref="MonotonicTimeProvider"/> to the <see cref="IServiceCollection"/> replacing any existing <see cref="TimeProvider"/> registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <see cref="IServiceCollection"/> to allow chained calls.</returns>
    public static IServiceCollection AddMonotonicClock(this IServiceCollection services)
    {
        var timeProviderDescriptor = new ServiceDescriptor(typeof(TimeProvider), MonotonicTimeProvider.Instance);
        services.Replace(timeProviderDescriptor);
        return services;
    }

    /// <summary>
    /// Adds the <see cref="MonotonicTimeProvider"/> to the <see cref="IServiceCollection"/> with the specified Service Key.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="key">The Service Key of the service.</param>
    /// <returns>The <see cref="IServiceCollection"/> to allow chained calls.</returns>
    public static IServiceCollection AddKeyedMonotonicClock(this IServiceCollection services, object key)
    {
        services.AddKeyedSingleton<TimeProvider>(key, MonotonicTimeProvider.Instance);
        return services;
    }
}
