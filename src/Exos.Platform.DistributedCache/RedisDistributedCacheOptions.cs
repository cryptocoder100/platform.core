#pragma warning disable CA2227 // Collection properties should be read only
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Exos.Platform.DistributedCache.Redis
{
    /// <summary>
    /// Configuration options for <see cref="RedisDistributedCache" />.
    /// </summary>
    public class RedisDistributedCacheOptions : IOptions<RedisDistributedCacheOptions>
    {
        /// <summary>
        /// Gets or sets the cache instance name.
        /// This allows partitioning a single Redis instance for use with multiple applications.
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// Gets or sets the number of times an operation will be retried if it fails.
        /// </summary>
        public int MaxRetries { get; set; } = RedisDistributedCacheDefaults.MaxRetries;

        /// <summary>
        /// Gets or sets the list of cache keys to apply fail fast feature (No retry operations for these keys).
        /// </summary>
        public List<string> FailFastKeys { get; set; } = RedisDistributedCacheDefaults.FailFastKeys;

        /// <inheritdoc/>
        public RedisDistributedCacheOptions Value => this;
    }
}
#pragma warning restore CA2227 // Collection properties should be read only