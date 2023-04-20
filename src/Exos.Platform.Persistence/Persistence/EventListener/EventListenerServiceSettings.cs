namespace Exos.Platform.Persistence.EventListener
{
    using System;

    /// <summary>
    /// Configuration settings for EventListenerService.
    /// </summary>
    public class EventListenerServiceSettings
    {
        /// <summary>
        /// Gets or sets how long to delay after the polling loop logic completes.
        /// </summary>
        public TimeSpan EventPollingInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets service Bus Connection String.
        /// </summary>
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// Gets or sets queue name.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        ///  Gets or sets maximum number of concurrent calls to the callback the message.
        /// </summary>
        public int MaxConcurrentCalls { get; set; }

        /// <summary>
        ///  Gets or sets topic Name.
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        ///  Gets or sets subscription Name.
        /// </summary>
        public string SubscriptionName { get; set; }
    }
}
