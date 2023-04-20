using System.Diagnostics.CodeAnalysis;
using Exos.Platform.RedisConnectionPool;
using Microsoft.Extensions.Caching.Distributed;

namespace Exos.Platform.DistributedCache.Redis.UnitTests.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class RedisDistributedCacheHelper
    {
        public static IDistributedCache CreateInstance(string instanceName)
        {
            var pool = new ConnectionMultiplexerPool(new ConnectionMultiplexerPoolOptions
            {
                Configuration = "AZE2-S-EXO-redApp01.redis.cache.windows.net:6380,password=AnPiMmMmiiIHr5mKKN8J2pGH9mv9rIYS+PYk32UET3o=,ssl=True,abortConnect=False,defaultDatabase=15",
                PoolSize = 3
            });

            return new RedisDistributedCache(new RedisDistributedCacheOptions { InstanceName = instanceName + ":", MaxRetries = 1 }, pool);
        }
    }
}
