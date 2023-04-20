#pragma warning disable SA1401 // Fields should be private
#pragma warning disable CA1051 // Do not declare visible instance fields
namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Concurrent;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Defines the <see cref="FailoverOnExcessiveExceptionPolicy{T}"/>.
    /// </summary>
    /// <typeparam name="T"><see cref="IClientEntity"/>.</typeparam>
    public abstract class FailoverOnExcessiveExceptionPolicy<T> : IFailoverPolicy<T> where T : IClientEntity
    {
        /// <summary>
        /// <see cref="ConcurrentHitCounter"/>.
        /// </summary>
        protected readonly ConcurrentHitCounter _hitCounter;

        /// <summary>
        /// <see cref="FailoverConfig"/>.
        /// </summary>
        protected readonly FailoverConfig _config;

        /// <summary>
        /// <see cref="ShouldFailover"/>.
        /// </summary>
        protected bool _shouldFailover;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverOnExcessiveExceptionPolicy{T}"/> class.
        /// </summary>
        /// <param name="config">The <see cref="FailoverConfig"/>.</param>
        protected FailoverOnExcessiveExceptionPolicy(FailoverConfig config)
        {
            _config = config;
            _hitCounter = _config == null
                ? new ConcurrentHitCounter()
                : new ConcurrentHitCounter(_config.SlidingDurationInSeconds);
        }

        /// <inheritdoc/>
        public bool ShouldFailover => _shouldFailover;

        /// <inheritdoc/>
        public virtual async Task<T> EnsureExecutionFailoverAsync(
            ExosClientEntityPool<T> clientPool, MessageEntity entity, bool useSecondary = false, Exception exception = null)
        {
            // Increment when primary namespace is active and publishing failed with an exception that is in scope
            if (_config.IsFailoverEnabled
                && !_shouldFailover && exception != null && _config.ExceptionNames.Contains(exception.GetType().FullName))
            {
                _hitCounter.Increment();
            }

            T client = await GetActiveClientEntity(clientPool, entity, useSecondary);

            if (_config.IsFailoverEnabled
                && !_shouldFailover && _hitCounter.GetCount() >= _config.ExceptionThreshold)
            {
                // this does not have to be thread safe.
                _shouldFailover = true;
            }

            return client;
        }

        /// <summary>
        /// Gets the client from <see cref="ExosClientEntityPool{T}"/> according to policy conditions.
        /// </summary>
        /// <param name="clientPool">The client pool from which client is retrieved.</param>
        /// <param name="entity">The configuration entity.</param>
        /// <param name="useSecondary">Optional, true indicating the current active secondary client should be used.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected virtual async Task<T> GetActiveClientEntity(
            ExosClientEntityPool<T> clientPool, MessageEntity entity, bool useSecondary = false)
        {
            _ = clientPool ?? throw new ArgumentNullException(nameof(clientPool));
            _ = entity ?? throw new ArgumentNullException(nameof(entity));

            // do not publish to the secondary when failover isn't enabled, or namespaces are the same
            // .
            // out of CAUTION that using different connection strings to connect to the same instance caused
            // intermittent issue in the past, it is a better choice to always pick the same connection string
            // as the one used by listener.
            if (!_config.IsFailoverEnabled
                || MessagingHelper.AreSameNamespaces(entity.ConnectionString, entity.PassiveConnectionString))
            {
                return clientPool.GetClientEntity(
                    entity.ConnectionString, entity.EntityName, ExosClientEntityPool<T>.GetRetryPolicy(entity.MaxRetryCount));
            }

            if (!_shouldFailover && !useSecondary)
            {
                // happy path the default primary namespace is the active primary
                return clientPool.GetClientEntity(
                    entity.ConnectionString, entity.EntityName, ExosClientEntityPool<T>.GetRetryPolicy(entity.MaxRetryCount));
            }
            else if (!_shouldFailover && useSecondary)
            {
                // publish through the primary namespace client throws exception, return the secondary
                // namespace client for retry
                return clientPool.GetClientEntity(
                    entity.PassiveConnectionString, entity.EntityName, ExosClientEntityPool<T>.GetRetryPolicy(entity.MaxRetryCount));
            }
            else if (MessagingHelper.AreSameNamespaces(entity.ConnectionString, entity.PassiveConnectionString))
            {
                // idealy this should not happen if switch is "off" given connection strings point to the same
                // instance. but if the swtich is "on", failover can happen.
                // .
                // out of CAUTION, use the same connection string through which listeners are connected
                return clientPool.GetClientEntity(
                    entity.ConnectionString, entity.EntityName, ExosClientEntityPool<T>.GetRetryPolicy(entity.MaxRetryCount));
            }
            else
            {
                // primary namespace is experiencing downtime, the secondary namespace acts as the new active primary
                // when first try to send message or first try has failed before failover switch is on and try second time after exception
                // disable the old primary namespace clients
                string primayClientToDisable = ExosClientEntityPool<T>.GetClientEntityName(entity.ConnectionString, entity.EntityName);
                bool isClosed = await clientPool.TryCloseClientEntityAsync(primayClientToDisable);

                if (!isClosed)
                {
                    // Ideally the primary client should be closed successfully, but during downtime if it does not this is ignored.
                }

                return clientPool.GetClientEntity(
                    entity.PassiveConnectionString, entity.EntityName, ExosClientEntityPool<T>.GetRetryPolicy(entity.MaxRetryCount));
            }
        }

        private bool ResetPolicy()
        {
            _shouldFailover = false;
            return _hitCounter.Reset();
        }
    }
}
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore CA1051 // Do not declare visible instance fields