namespace Exos.Platform.TenancyHelper.Utils
{
    using Microsoft.Extensions.Caching.Distributed;

    /// <summary>
    /// Return the implementation of Distributed Cache.
    /// </summary>
    public interface ICacheFactory
    {
        /// <summary>
        /// Get Distributed Cache Provider.
        /// </summary>
        /// <returns>IDistributedCache.</returns>
        IDistributedCache GetCacheProvider();
    }

    /// <inheritdoc/>
    public interface ICacheFactoryInstance1 : ICacheFactory
    {
    }

    /// <inheritdoc/>
    public interface ICacheFactoryInstance2 : ICacheFactory
    {
    }

    /// <inheritdoc/>
    public interface ICacheFactoryInstance3 : ICacheFactory
    {
    }

    /// <inheritdoc/>
    public interface ICacheFactoryInstance4 : ICacheFactory
    {
    }
}