#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.ICTLibrary.Repository.Model
{
    using System;

    /// <summary>
    /// Event Entity Topic.
    /// </summary>
    public partial class EventEntityTopic
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
        /// Gets or sets CreatedDate.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets CreatedBy.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedDate.
        /// </summary>
        public DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedBy.
        /// </summary>
        public string LastUpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets Version.
        /// </summary>
        public byte[] Version { get; set; }

        /// <summary>
        /// Gets or sets Publisher.
        /// </summary>
        public EventPublisher Publisher { get; set; }

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
#pragma warning restore CA1819 // Properties should not return arrays