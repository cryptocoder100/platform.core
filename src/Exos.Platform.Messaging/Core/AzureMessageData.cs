namespace Exos.Platform.Messaging.Core
{
    using System;

    /// <summary>
    /// This model is used as the data model for azure message.
    /// We get the payload and enhance it to Azure Message.
    /// </summary>
    public class AzureMessageData
    {
        /// <summary>
        /// Gets or sets TopicName.
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// Gets or sets Message.
        /// </summary>
        public MessageData Message { get; set; }

        /// <summary>
        /// Gets or sets PublishTime.
        /// </summary>
        public DateTime PublishTime { get; set; }

        /// <summary>
        /// Gets or sets MessageGuid.
        /// </summary>
        public Guid MessageGuid { get; set; }
    }
}
