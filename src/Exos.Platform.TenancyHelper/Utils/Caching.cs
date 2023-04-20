#pragma warning disable SA1402 // FileMayOnlyContainASingleType
#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.TenancyHelper.Utils
{
    using System;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.TenancyHelper.PersistenceService;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class DocPolicyCacheFactory : ICacheFactory
    {
        private readonly RepositoryOptions _repositoryOptions;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocPolicyCacheFactory"/> class.
        /// </summary>
        /// <param name="repositoryOptions">RepositoryOptions.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="logger">ILogger.</param>
        public DocPolicyCacheFactory(IOptions<RepositoryOptions> repositoryOptions, IDistributedCache distributedCache, ILogger<DocPolicyCacheFactory> logger)
        {
            if (repositoryOptions == null)
            {
                throw new ArgumentNullException(nameof(repositoryOptions));
            }

            _repositoryOptions = repositoryOptions.Value ?? throw new ArgumentNullException(nameof(repositoryOptions));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public IDistributedCache GetCacheProvider()
        {
            if (string.IsNullOrEmpty(_repositoryOptions.PolicyDocumentsCacheProvider))
            {
                return new InMemoryCacheProvider(_logger);
            }
            else
            {
                switch (_repositoryOptions.PolicyDocumentsCacheProvider)
                {
                    case "inmemory":
                        {
                            return new InMemoryCacheProvider(_logger);
                        }

                    case "redis":
                        {
                            return _distributedCache;
                        }

                    default:
                        {
                            return _distributedCache;
                        }
                }
            }
        }
    }

    /// <inheritdoc/>
    public class InMemoryCacheProvider : IDistributedCache
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryCacheProvider"/> class.
        /// </summary>
        /// <param name="logger">ILogger.</param>
        public InMemoryCacheProvider(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public byte[] Get(string key)
        {
            try
            {
                // Get the default MemoryCache
                ObjectCache cache = MemoryCache.Default;

                // Get object from cache
                return (byte[])cache.Get(key);
            }
            catch (Exception ex)
            {
                // Ignore any exception while putting to MemoryCache
                _logger.LogError(ex, $"Error Getting Key {LoggerHelper.SanitizeValue(key)} from InMemoryCacheProvider");
            }

            return null;
        }

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Add a string key-value to memory cache.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="options">DistributedCacheEntryOptions.</param>
        /// <param name="token">CancellationToken.</param>
        public void SetString(string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            try
            {
                // Put the object to memory cache only if it is valid
                if (value != null)
                {
                    // Get the default MemoryCache
                    ObjectCache cache = MemoryCache.Default;

                    // Set the cache eviction policy
                    CacheItemPolicy policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(3 * 60 * 60),
                    };

                    // Put the object to cache
                    cache.Add(key, value, policy);
                }
            }
            catch (Exception ex)
            {
                // Ignore any exception while putting to MemoryCache
                _logger.LogError(ex, $"Error Adding Key {LoggerHelper.SanitizeValue(key)} - Value {LoggerHelper.SanitizeValue(value)} from InMemoryCacheProvider");
            }
        }

        /// <summary>
        /// Get a string key.
        /// </summary>
        /// <param name="key">Key value.</param>
        /// <param name="token">CancellationToken.</param>
        /// <returns>Value that match the key.</returns>
        public string GetString(string key, CancellationToken token = default(CancellationToken))
        {
            try
            {
                // Get the default MemoryCache
                ObjectCache cache = MemoryCache.Default;

                // Get object from cache
                return (string)cache.Get(key);
            }
            catch (Exception ex)
            {
                // Ignore any exception while putting to MemoryCache
                _logger.LogError(ex, $"Error Getting Key {LoggerHelper.SanitizeValue(key)} from InMemoryCacheProvider");
            }

            return null;
        }

        /// <inheritdoc/>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try
            {
                // Put the object to memory cache only if it is valid
                if (value != null)
                {
                    // Get the default MemoryCache
                    ObjectCache cache = MemoryCache.Default;

                    // Set the cache eviction policy
                    CacheItemPolicy policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(3 * 60 * 60),
                    };

                    // Put the object to cache
                    cache.Add(key, value, policy);
                }
            }
            catch (Exception ex)
            {
                // Ignore any exception while putting to MemoryCache
                _logger.LogError(ex, $"Error Getting Key {LoggerHelper.SanitizeValue(key)} from InMemoryCacheProvider");
            }
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }

    /// <inheritdoc/>
    public class DocPolicyCacheFactoryInstance1 : DocPolicyCacheFactory, ICacheFactoryInstance1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPolicyCacheFactoryInstance1"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="logger">ILogger.</param>
        public DocPolicyCacheFactoryInstance1(IOptions<RepositoryOptions> options, IDistributedCache distributedCache, ILogger<DocPolicyCacheFactoryInstance1> logger) : base(options, distributedCache, logger)
        {
        }
    }

    /// <inheritdoc/>
    public class DocPolicyCacheFactoryInstance2 : DocPolicyCacheFactory, ICacheFactoryInstance2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPolicyCacheFactoryInstance2"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="logger">ILogger.</param>
        public DocPolicyCacheFactoryInstance2(IOptions<RepositoryOptions> options, IDistributedCache distributedCache, ILogger<DocPolicyCacheFactoryInstance2> logger) : base(options, distributedCache, logger)
        {
        }
    }

    /// <inheritdoc/>
    public class DocPolicyCacheFactoryInstance3 : DocPolicyCacheFactory, ICacheFactoryInstance3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPolicyCacheFactoryInstance3"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="logger">ILogger.</param>
        public DocPolicyCacheFactoryInstance3(IOptions<RepositoryOptions> options, IDistributedCache distributedCache, ILogger<DocPolicyCacheFactoryInstance3> logger) : base(options, distributedCache, logger)
        {
        }
    }

    /// <inheritdoc/>
    public class DocPolicyCacheFactoryInstance4 : DocPolicyCacheFactory, ICacheFactoryInstance4
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPolicyCacheFactoryInstance4"/> class.
        /// </summary>
        /// <param name="options">RepositoryOptions.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="logger">ILogger.</param>
        public DocPolicyCacheFactoryInstance4(IOptions<RepositoryOptions> options, IDistributedCache distributedCache, ILogger<DocPolicyCacheFactoryInstance4> logger) : base(options, distributedCache, logger)
        {
        }
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning restore CA1031 // Do not catch general exception types