using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class NamedServiceRegistration<TService> : IDisposable
{
    private readonly object _lockObj = new object();
    private readonly Func<IServiceProvider, TService> _factory;

    private TService _service;
    private ExceptionDispatchInfo _exception;

    public NamedServiceRegistration(string name, Func<IServiceProvider, TService> factory)
    {
        Debug.Assert(!string.IsNullOrEmpty(name));
        Debug.Assert(factory != null);

        Name = name;
        _factory = factory;
    }

    public string Name { get; }

    public void Dispose()
    {
        if (_service is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public TService MaterializeService(IServiceProvider serviceProvider)
    {
        Debug.Assert(serviceProvider != null);

        // Get an instance of the service in a thread-safe way.
        // We capture any exceptions and rethrow then on subsequent attempts.

        _exception?.Throw();
        if (_service != null)
        {
            return _service;
        }

        lock (_lockObj)
        {
            _exception?.Throw();
            if (_service != null)
            {
                return _service;
            }

            try
            {
                _service = _factory(serviceProvider);
            }
            catch (Exception ex)
            {
                _exception = ExceptionDispatchInfo.Capture(ex);
                throw;
            }
        }

        return _service;
    }
}
