using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Exos.Platform.Messaging.Core.Listener
{
    /// <summary>
    /// Message Listener configuration.
    /// </summary>
    public interface IExosMessageListener
    {
        /// <summary>
        /// Call this method for registering to a queue or topic with appropriate configuration
        /// Subscription name has to be there to register as a topic listener.
        /// </summary>
        /// <param name="listenerConfig">MessageListenerConfig.</param>
        /// <param name="clientProcessor">MessageProcessor.</param>
        /// <returns>A collection of MessageConsumers.</returns>
        ICollection<MessageConsumer> RegisterServiceBusEntityListener(MessageListenerConfig listenerConfig, MessageProcessor clientProcessor);

        /// <summary>
        /// Call this method if you want to read from the config and start the processor.
        /// </summary>
        void StartListener();

        /// <summary>
        /// Start Listener.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider.</param>
        void StartListener(IServiceProvider serviceProvider);

        /// <summary>
        /// Method to call stop and remove all the listeners when the app shutdown happens.
        /// </summary>
        Task StopListener();

        /// <summary>
        /// Stop listener for the topic and subscription specified.
        /// </summary>
        /// <param name="topicName">topic name.</param>
        /// <param name="subscriptionName">subscription name.</param>
        /// <returns><see cref="Task"/>.</returns>
        Task StopListener(string topicName, string subscriptionName);

        /// <summary>
        /// This will go through the message entity table and create listeners for AllMessages subscription.
        /// </summary>
        void StartAllEntityListeners();

        /// <summary>
        /// Start all Entity Listeners.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider.</param>
        void StartAllEntityListeners(IServiceProvider serviceProvider);

        /// <summary>
        /// Start a specific listener.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider.</param>
        /// <param name="topicName">topic name.</param>
        /// <param name="subscriptionName">subscription name.</param>
        void StartEntityListener(IServiceProvider serviceProvider, string topicName, string subscriptionName);

         /// <summary>
        /// Get all active entity listeners.
        /// </summary>
        ICollection<string> GetActiveEntityListeners();
    }
}
