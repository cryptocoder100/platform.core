namespace Exos.Platform.ICTLibrary.Core.Model
{
    /// <summary>
    /// Event Entity Topic.
    /// </summary>
    public class EventEntityTopicCache
    {
        /// <summary>
        /// Gets or sets EventEntityTopicId.
        /// </summary>
        public int EventEntityTopicId { get; set; }

        /// <summary>
        /// Gets or sets PublisherId.
        /// </summary>
        public short PublisherId { get; set; }

        /// <summary>
        /// Gets or sets PublisherName.
        /// </summary>
        public string PublisherName { get; set; }

        /// <summary>
        /// Gets or sets EventName.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets EventNameDescrption.
        /// </summary>
        public string EventNameDescrption { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets EntityNameDescription.
        /// </summary>
        public string EntityNameDescription { get; set; }

        /// <summary>
        /// Gets or sets TopicName.
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IsPublishToServiceBusActive.
        /// </summary>
        public bool IsPublishToServiceBusActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IsPublishToEventHubActive.
        /// </summary>
        public bool IsPublishToEventHubActive { get; set; }
    }
}
