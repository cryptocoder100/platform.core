namespace Exos.Platform.Messaging.Helper
{
    using Exos.Platform.AspNetCore.Logging;

    /// <summary>
    /// Message Logging Events.
    /// </summary>
    public class MessagingLoggingEvent : PlatformEvents
    {
        /// <summary>
        /// Define the application's id and base event for it.
        /// </summary>
        public new const int AppId = 555; // IMPORTANT: Must be unique for all application.

        /// <summary>
        /// Base Event.
        /// </summary>
        public new const int Base = (AppId * Range) + Offset;

        /// <summary>
        /// Generic Exception Event.
        /// </summary>
        public const int GenericExceptionInMessaging = Base + 0;

        /// <summary>
        /// Topic Or Queue Not Found Event.
        /// </summary>
        public const int TopicOrQueueNotFound = Base + 1;

        /// <summary>
        /// Incoming Null Message Event.
        /// </summary>
        public const int IncomingMessageObjectNull = Base + 2;

        /// <summary>
        /// Failure Writing to Azure Service Bus Event.
        /// </summary>
        public const int WritingToAzureServiceBusFailed = Base + 3;

        /// <summary>
        /// Listener can't be registered Event.
        /// </summary>
        public const int CantRegisterListener = Base + 4;

        /// <summary>
        /// Publish Failed to Primary Service Bus Event.
        /// </summary>
        public const int PublishFailedToPrimary = Base + 5;

        /// <summary>
        ///  Publish Failed to Primary and Secondary Service Bus Event.
        /// </summary>
        public const int PublishFailedToPrimaryAndSecondary = Base + 6;

        /// <summary>
        /// Fail to add a Log Entry for Subscription Event.
        /// </summary>
        public const int SubscriptionLogEntryFailed = Base + 7;

        /// <summary>
        /// Disabled Subscription Event.
        /// </summary>
        public const int SubscriptionDisabled = Base + 8;

        /// <summary>
        /// Failed Writing To Azure EventHub Event.
        /// </summary>
        public const int WritingToAzureEventHubFailed = Base + 9;
    }
}
