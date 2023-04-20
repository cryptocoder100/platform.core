using System;
using Exos.Platform.RedisConnectionPool;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection" /> that configure a Redis connection pool.
    /// </summary>
    public static class ConnectionMultiplexerPoolServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a Redis connection pool to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{ConnectionMultiplexerPoolOptions}" /> to configure the provided <see cref="ConnectionMultiplexerPoolOptions" />.</param>
        /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddExosRedisConnectionPool(this IServiceCollection services, Action<ConnectionMultiplexerPoolOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);
            services.AddSingleton<IConnectionMultiplexerPool, ConnectionMultiplexerPool>();

            return services;
        }
    }
}
