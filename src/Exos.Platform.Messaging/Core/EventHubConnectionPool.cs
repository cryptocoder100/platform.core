namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// EventHubConnectionPool.
    /// </summary>
    public static class EventHubConnectionPool
    {
        /// <summary>
        /// Will hold the connections.
        /// </summary>
        private static ConcurrentDictionary<string, EventHubClient> _eventHubConnectionPool = new ConcurrentDictionary<string, EventHubClient>();

        /// <summary>
        /// This gets the connection based on Namespace and Entity.
        /// </summary>
        /// <param name="aliasNamespace">alias name space.</param>
        /// <param name="entityName">entity name.</param>
        /// <param name="eventHubConnectionString">EventHub connection string.</param>
        /// <returns>Connection object.</returns>
        public static EventHubClient GetConnection(string aliasNamespace, string entityName, string eventHubConnectionString)
        {
            return _eventHubConnectionPool.GetOrAdd(eventHubConnectionString + aliasNamespace + entityName, (keyConnection) =>
            {
                // Basic connection.
                EventHubsConnectionStringBuilder connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString)
                {
                    EntityPath = entityName,
                };

                // Create connection
                EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

                // Retry policy
                eventHubClient.RetryPolicy = GetEventHubRetryPolicy(10);

                return eventHubClient;
            });
        }

        /// <summary>
        /// This gets the connection based on Namespace and Entity.
        /// </summary>
        /// <param name="aliasNamespace">alias name space.</param>
        /// <param name="entityName">entity name.</param>
        /// <param name="eventHubEndpoint">EventHub url.</param>
        /// <returns>Connection object.</returns>
        public static EventHubClient GetManagedConnection(string aliasNamespace, string entityName, string eventHubEndpoint)
        {
            return _eventHubConnectionPool.GetOrAdd(eventHubEndpoint + aliasNamespace + entityName, (keyConnection) =>
            {
                EventHubClient eventHubClient = EventHubClient.CreateWithManagedIdentity(new Uri(eventHubEndpoint), entityName);

                eventHubClient.RetryPolicy = GetEventHubRetryPolicy(10);

                return eventHubClient;
            });
        }

        /// <summary>
        /// Retry policy.
        /// </summary>
        /// <param name="retryCount">Retry count.</param>
        /// <returns>Event hub retry policy.</returns>
        private static Microsoft.Azure.EventHubs.RetryPolicy GetEventHubRetryPolicy(int retryCount)
        {
            var retry = new Microsoft.Azure.EventHubs.RetryExponential(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), retryCount);
            return retry;
        }
    }
}
