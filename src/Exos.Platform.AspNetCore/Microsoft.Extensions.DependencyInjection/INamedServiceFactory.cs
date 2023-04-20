using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A factory abstraction for creating named services.
/// </summary>
/// <typeparam name="TService">The type of named service.</typeparam>
public interface INamedServiceFactory<TService> where TService : class
{
    /// <summary>
    /// Returns a <typeparamref name="TService"/> instance that corresponds to the logical <paramref name="name" /> specified.
    /// </summary>
    /// <param name="name">The logical name of the service.</param>
    /// <returns>A <typeparamref name="TService" /> instance.</returns>
    TService GetService(string name);
}
