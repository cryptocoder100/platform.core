namespace Exos.Platform.Messaging.Core
{
    using System;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Primitives;

    /// <summary>
    /// Defines the <see cref="ExosTopicClientPool"/>.
    /// </summary>
    public class ExosTopicClientPool : ExosClientEntityPool<TopicClient>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExosTopicClientPool"/> class.
        /// </summary>
        /// <param name="tokenProvider">The token provider for MSI.</param>
        public ExosTopicClientPool(ITokenProvider tokenProvider) : base(tokenProvider)
        {
        }

        /// <inheritdoc/>
        public override TopicClient GetClientEntity(
            string namespaceName, string entityName, RetryPolicy retryPolicy = null)
        {
            if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(entityName))
            {
                // do nothing.
            }

            retryPolicy ??= GetRetryPolicy();

            bool isManagedIdentity = !MessageEntity.ConnectionStringHasKeys(namespaceName);
            string topicClientName = GetClientEntityName(namespaceName, entityName);

            TopicClient client = isManagedIdentity
                ? ClientEntities.GetOrAdd(
                    topicClientName,
                    (key) => new TopicClient(namespaceName, entityName, TokenProvider, TransportType.Amqp, retryPolicy))
                : ClientEntities.GetOrAdd(
                    topicClientName,
                    (key) => new TopicClient(namespaceName, entityName, retryPolicy));

            client.OperationTimeout = TimeSpan.FromSeconds(DefaultOperationTimeoutInSeconds);
            return client;
        }
    }
}
