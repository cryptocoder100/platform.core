using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder for named services.
/// </summary>
/// <typeparam name="TService">The type of named service.</typeparam>
public class NamedServiceFactoryBuilder<TService>
{
    private readonly IServiceCollection _services;

    internal NamedServiceFactoryBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
    }

    /// <summary>
    /// Adds a service with an implementation type specified in <typeparamref name="TImplementation" />.
    /// </summary>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="name">The logical name of the service.</param>
    /// <returns>The updated <see cref="NamedServiceFactoryBuilder{TService}" />.</returns>
    public NamedServiceFactoryBuilder<TService> Add<TImplementation>(string name) where TImplementation : TService
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

#pragma warning disable CA2000 // Dispose objects before losing scope

        // Registration lifetime and disposal is managed by the service container
        _services.AddSingleton(new NamedServiceRegistration<TService>(name, sp => ActivatorUtilities.CreateInstance<TImplementation>(sp)));

#pragma warning restore CA2000 // Dispose objects before losing scope

        return this;
    }

    /// <summary>
    /// Adds a service using the factory specified in <paramref name="factory" />.
    /// </summary>
    /// <param name="name">The logical name of the service.</param>
    /// <param name="factory">The factory that creates the service.</param>
    /// <returns>The updated <see cref="NamedServiceFactoryBuilder{TService}" />.</returns>
    public NamedServiceFactoryBuilder<TService> Add(string name, Func<IServiceProvider, TService> factory)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        ArgumentNullException.ThrowIfNull(factory);

#pragma warning disable CA2000 // Dispose objects before losing scope

        // Registration lifetime and disposal is managed by the service container
        _services.AddSingleton(new NamedServiceRegistration<TService>(name, factory));

#pragma warning restore CA2000 // Dispose objects before losing scope

        return default;
    }
}
