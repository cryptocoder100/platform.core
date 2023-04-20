using System.Collections.Generic;

namespace Exos.Platform.DistributedCache.Redis
{
    /// <summary>
    /// Default values for <see cref="RedisDistributedCacheOptions" />.
    /// </summary>
    public static class RedisDistributedCacheDefaults
    {
        /// <summary>
        /// Gets the maximum number of times to retry a failed operation.
        /// </summary>
        public static int MaxRetries { get; } = 6;

        /// <summary>
        /// Gets the list of cache keys to apply fail fast feature (No retry operations for these keys).
        /// </summary>
        public static List<string> FailFastKeys { get; } = new List<string> { "UserClaimsCacheKey:", "UserClaimsWorkOrdersCacheKey:", "IsRevokedTokenKey:" };
    }
}
