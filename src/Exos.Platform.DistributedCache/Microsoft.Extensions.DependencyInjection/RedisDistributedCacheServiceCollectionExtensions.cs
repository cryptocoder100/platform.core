using System;
using Exos.Platform.DistributedCache.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection" /> that configures Redis distributed cache related services.
    /// </summary>
    public static class RedisDistributedCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Redis distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">Optional. An <see cref="Action{RedisDistributedCacheOptions}" /> to configure the provided <see cref="RedisDistributedCacheOptions" />.</param>
        /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddExosRedisDistributedCache(this IServiceCollection services, Action<RedisDistributedCacheOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.Configure(setupAction ?? new Action<RedisDistributedCacheOptions>(o => { }));
            services.AddSingleton<IDistributedCache, RedisDistributedCache>();

            return services;
        }
    }
}
