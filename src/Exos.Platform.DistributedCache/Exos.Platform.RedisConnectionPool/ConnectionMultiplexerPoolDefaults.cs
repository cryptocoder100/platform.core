namespace Exos.Platform.RedisConnectionPool
{
    /// <summary>
    /// Default values for <see cref="ConnectionMultiplexerPoolOptions" />.
    /// </summary>
    public static class ConnectionMultiplexerPoolDefaults
    {
        /// <summary>
        /// Gets the default connection pool size.
        /// </summary>
        public static int PoolSize { get; } = 3;
    }
}
