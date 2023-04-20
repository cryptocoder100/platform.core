using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Exos.Platform.RedisConnectionPool
{
    /// <summary>
    /// Represents a pool of Redis multiplexer connections.
    /// </summary>
    public interface IConnectionMultiplexerPool
    {
        /// <summary>
        /// Retrieves a multiplexer connection from the pool.
        /// </summary>
        /// <returns>An established <see cref="Task{IConnectionMultiplexer}" /> from the pool.</returns>
        Task<IConnectionMultiplexer> GetConnectionAsync();

        /// <summary>
        /// Removes a failed multiplexer connection from the pool.
        /// A new connection will be created to replace the failed one.
        /// </summary>
        /// <param name="connection">A failed <see cref="IConnectionMultiplexer" />.</param>
        /// <param name="exception">The exception that caused the connection to fail.</param>
        void FailConnection(IConnectionMultiplexer connection, Exception exception);
    }
}
