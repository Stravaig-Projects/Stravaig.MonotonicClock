using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stravaig.MonotonicClock.DependencyInjection;

/// <summary>
/// Extension methods for the ServiceCollection to add the Monotonic Clock to the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Monotonic Time Provider to the DI container if a TimeProvider has not already been added.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection to allow chained calls.</returns>
    public static IServiceCollection TryAddMonotonicClock(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(MonotonicTimeProvider.Instance);
        return services;
    }

    /// <summary>
    /// Adds the Monotonic Time Provider to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection to allow chained calls.</returns>
    public static IServiceCollection AddMonotonicClock(this IServiceCollection services)
    {
        var timeProviderDescriptor = new ServiceDescriptor(typeof(TimeProvider), _ => MonotonicTimeProvider.Instance, ServiceLifetime.Singleton);
        services.Replace(timeProviderDescriptor);
        return services;
    }

    /// <summary>
    /// Adds the Monotonic Time Provider to the DI container with the specified Service Key.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="key">The Service Key of the service.</param>
    /// <returns>The service collection to allow chained calls.</returns>
    public static IServiceCollection AddKeyedMonotonicClock(this IServiceCollection services, object key)
    {
        services.AddKeyedSingleton<TimeProvider>(key, MonotonicTimeProvider.Instance);
        return services;
    }
}
