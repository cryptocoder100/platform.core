namespace Exos.Platform.Messaging.Core.Listener
{
    /// <summary>
    /// Message Listener Configuration.
    /// </summary>
    public class MessageListenerConfig
    {
        /// <summary>
        ///  Gets or sets a value for Azure entity name , Topic or Queue.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets a value for Entity Owner, Each micro service or module's name who has access to the entity.
        /// </summary>
        public string EntityOwner { get; set; }

        /// <summary>
        /// Gets or sets a value for Entity Description.
        /// </summary>
        public string EntityDescription { get; set; }

        /// <summary>
        /// Gets or sets a value for the Number of threads listening to the entity.
        /// </summary>
        public int NumberOfThreads { get; set; }

        /// <summary>
        /// Gets or sets a value for Retry count. This should be equal to the Max Delivery count in subscription or queue.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets a value for subscription name if it is topic. Subscription is empty, it is queue listener.
        /// </summary>
        public string SubscriptionName { get; set; }

        /// <summary>
        /// Gets or sets a value fir Processor name to process the consumed messages.
        /// </summary>
        public string Processor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enable and disable , default is false.
        /// </summary>
        public bool DisabledFlg { get; set; }

        /// <summary>
        /// Gets or sets, for now, all the consumers will point to one location. If it becomes event/entity specific, we may move it to MessagingDb along with ServiceBus/EventHub connection string.
        /// </summary>
        public string ConsumerBlobLocation { get; set; }

        /// <summary>
        /// Gets or sets, for now, all the consumers will point to one location. If it becomes event/entity specific, we may move it to MessagingDb along with ServiceBus/EventHub connection string.
        /// </summary>
        public string ConsumerBlobContainerName { get; set; }

        /// <summary>
        /// Gets or sets the number of consumer instance in one single machine.
        /// </summary>
        public int InstanceCount { get; set; }
    }
}
