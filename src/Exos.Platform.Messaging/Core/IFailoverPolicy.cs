namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Defines the <see cref="IFailoverPolicy{T}"/>.
    /// This policy defines the failover condition and determines if a failover is detected.
    /// </summary>
    /// <typeparam name="T"><see cref="ClientEntity"/>.</typeparam>
    public interface IFailoverPolicy<T> where T : IClientEntity
    {
        /// <summary>
        /// Gets a value indicating whether a failover condition is met.
        /// </summary>
        bool ShouldFailover { get; }

        /// <summary>
        /// Gets the client to be used based on the failover policy.
        /// </summary>
        /// <param name="clientPool">The <see cref="ExosClientEntityPool{T}"/>.</param>
        /// <param name="entity">The <see cref="MessageEntity"/> provide metadata of a message entity.</param>
        /// <param name="useSecondary">True if the secondary namespace should be used.</param>
        /// <param name="exception">The exception object from a failure publishing to the primary namespace.</param>
        /// <returns>The <typeparamref name="T"/> instance.</returns>
        Task<T> EnsureExecutionFailoverAsync(
            ExosClientEntityPool<T> clientPool, MessageEntity entity, bool useSecondary = false, Exception exception = null);
    }
}
