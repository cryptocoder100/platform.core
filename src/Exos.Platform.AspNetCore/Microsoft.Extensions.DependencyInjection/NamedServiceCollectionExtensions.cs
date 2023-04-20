using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering named services.
/// </summary>
public static class NamedServiceCollectionExtensions
{
    /// <summary>
    /// Adds a singleton service factory of the type specified in <typeparamref name="TService" /> to the
    /// <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="builder">A <see cref="NamedServiceFactoryBuilder{TService}" /> that can be used to register named services.</param>
    /// <returns>The updated <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddNamedSingleton<TService>(this IServiceCollection services, Action<NamedServiceFactoryBuilder<TService>> builder) where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(builder);

        // Ensure we have a factory for this service type
        services.TryAddSingleton<INamedServiceFactory<TService>, NamedServiceFactory<TService>>();

        // Pass the caller a builder to help them register named services
        var factory = new NamedServiceFactoryBuilder<TService>(services);
        builder(factory);

        return services;
    }
}
