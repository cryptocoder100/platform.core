using Microsoft.Extensions.Options;

namespace Exos.Platform.RedisConnectionPool
{
    /// <summary>
    /// Configuration options for <see cref="ConnectionMultiplexerPool" />.
    /// </summary>
    public class ConnectionMultiplexerPoolOptions : IOptions<ConnectionMultiplexerPoolOptions>
    {
        /// <summary>
        /// Gets or sets the configuration used to connect to Redis.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets or sets the number of Redis connections in the pool.
        /// </summary>
        public int PoolSize { get; set; } = ConnectionMultiplexerPoolDefaults.PoolSize;

        /// <summary>
        /// Gets the configuration for ConnectionMultiplexerPoolOptions.
        /// </summary>
        public ConnectionMultiplexerPoolOptions Value => this;
    }
}
