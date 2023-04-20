#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1000 // Do not declare static members on generic types
namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Primitives;

    /// <summary>
    /// Defines the <see cref="ExosClientEntityPool{T}"/>.
    /// </summary>
    /// <typeparam name="T"><see cref="IClientEntity"/>.</typeparam>
    public abstract class ExosClientEntityPool<T> : IExosClientEntityPool<T> where T : IClientEntity
    {
        /// <summary>
        /// DefaultRetryCount.
        /// </summary>
        protected const int _defaultRetryCount = 3;

        /// <summary>
        /// DefaultOperationTimeoutInSeconds.
        /// </summary>
        protected const int _defaultOperationTimeoutInSeconds = 10;

        private readonly ConcurrentDictionary<string, T> _clientEntities;
        private readonly ITokenProvider _tokenProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosClientEntityPool{T}"/> class.
        /// </summary>
        /// <param name="tokenProvider">The token provider for MSI.</param>
        protected ExosClientEntityPool(ITokenProvider tokenProvider)
        {
            _clientEntities = new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            _tokenProvider = tokenProvider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosClientEntityPool{T}"/> class.
        /// This constructor is for testing only.
        /// </summary>
        protected ExosClientEntityPool()
        {
        }

        /// <summary>
        /// Gets the default operation timeout in seconds.
        /// </summary>
        public static int DefaultOperationTimeoutInSeconds { get; } = _defaultOperationTimeoutInSeconds;

        /// <summary>
        /// Gets the default retry count.
        /// </summary>
        public static int DefaultRetryCount { get => _defaultRetryCount; }

        /// <summary>
        /// Gets the client entity dictionary.
        /// </summary>
        protected ConcurrentDictionary<string, T> ClientEntities { get => _clientEntities; }

        /// <summary>
        /// Gets the token provider.
        /// </summary>
        protected ITokenProvider TokenProvider { get => _tokenProvider; }

        /// <summary>
        /// GetRetryPolicy.
        /// </summary>
        /// <param name="retryCount">retryCount.</param>
        /// <returns>retry policy.</returns>
        public static RetryPolicy GetRetryPolicy(int retryCount = 0)
        {
            if (retryCount == 0)
            {
                retryCount = _defaultRetryCount;
            }

            var retry = new RetryExponential(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), retryCount);
            return retry;
        }

        /// <summary>
        /// Gets the name of a client.
        /// </summary>
        /// <param name="namespaceName">The namespace name used in the name of the client.</param>
        /// <param name="entityName">The entity name used in the name of the client.</param>
        /// <returns>The name created with <paramref name="namespaceName"/> and <paramref name="entityName"/>.</returns>
        public static string GetClientEntityName(string namespaceName, string entityName)
        {
            return $"{namespaceName}:{entityName}";
        }

        /// <inheritdoc/>
        public abstract T GetClientEntity(
            string namespaceName, string entityName, RetryPolicy retryPolicy = null);

        /// <inheritdoc/>
        public virtual bool ContainsClientEntity(string clientName)
        {
            return _clientEntities.ContainsKey(clientName);
        }

        /// <inheritdoc/>
        public virtual async Task<bool> TryCloseClientEntityAsync(string clientName)
        {
            bool isClosed = true, isRemoved = true;
            if (_clientEntities.TryGetValue(clientName, out T topicClient)
                && topicClient != null && !topicClient.IsClosedOrClosing)
            {
                try
                {
                    await topicClient.CloseAsync();
                    isRemoved = _clientEntities.TryRemove(clientName, out topicClient);
                }
                catch
                {
                    isClosed = false;
                }
            }

            return isClosed && isRemoved;
        }
    }
}
#pragma warning restore CA1000 // Do not declare static members on generic types
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore CA1031 // Do not catch general exception types