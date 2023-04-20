namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Primitives;

    /// <summary>
    /// Defines the <see cref="ExosQueueClientPool"/>.
    /// </summary>
    public class ExosQueueClientPool : ExosClientEntityPool<QueueClient>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExosQueueClientPool"/> class.
        /// </summary>
        /// <param name="tokenProvider">The token provider for MSI.</param>
        public ExosQueueClientPool(ITokenProvider tokenProvider) : base(tokenProvider)
        {
        }

        /// <summary>
        /// This method returns a new <see cref="QueueClient"/> everytime.
        /// </summary>
        /// <param name="namespaceName">The namespace name used in the name of the client.</param>
        /// <param name="entityName">The entity name used in the name of the client.</param>
        /// <param name="retryPolicy">The retry policy to be applied to the client.</param>
        /// <returns>The <see cref="QueueClient"/> instance.</returns>
        public override QueueClient GetClientEntity(
            string namespaceName, string entityName, RetryPolicy retryPolicy = null)
        {
            if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(entityName))
            {
                // do nothing.
            }

            bool isManagedIdentity = !MessageEntity.ConnectionStringHasKeys(namespaceName);

            QueueClient client = isManagedIdentity
                ? new QueueClient(namespaceName, entityName, TokenProvider)
                : new QueueClient(namespaceName, entityName);

            client.OperationTimeout = TimeSpan.FromSeconds(DefaultOperationTimeoutInSeconds);
            return client;
        }

        /// <summary>
        /// Clients are not pooled.
        /// </summary>
        /// <param name="clientName">The name to locate in the <see cref="ExosQueueClientPool"/>.</param>
        /// <returns>false.</returns>
        public override bool ContainsClientEntity(string clientName)
        {
            return false;
        }

        /// <summary>
        /// Clients are not pooled.
        /// </summary>
        /// <param name="clientName">The name of the client to be removed.</param>
        /// <returns>true.</returns>
        public override async Task<bool> TryCloseClientEntityAsync(string clientName)
        {
            return await Task.FromResult(true);
        }
    }
}
