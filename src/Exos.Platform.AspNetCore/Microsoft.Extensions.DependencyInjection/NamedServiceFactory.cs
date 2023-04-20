using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class NamedServiceFactory<TService> : IDisposable, INamedServiceFactory<TService> where TService : class
{
    private readonly Dictionary<string, NamedServiceRegistration<TService>> _services;
    private readonly IServiceProvider _serviceProvider;

    public NamedServiceFactory(
        IServiceProvider serviceProvider,
        IEnumerable<NamedServiceRegistration<TService>> registrations)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(registrations);

        _serviceProvider = serviceProvider;

        // Create a service lookup table by name
        _services = new Dictionary<string, NamedServiceRegistration<TService>>();
        foreach (var registration in registrations)
        {
            _services[registration.Name] = registration;
        }
    }

    public void Dispose()
    {
        foreach (var service in _services)
        {
            service.Value?.Dispose();
        }
    }

    public TService GetService(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (!_services.TryGetValue(name, out NamedServiceRegistration<TService> service))
        {
            throw new InvalidOperationException($"Unable to find named service '{name}' with type '{typeof(TService).Name}'.");
        }

        return service.MaterializeService(_serviceProvider);
    }
}
