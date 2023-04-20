#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.ICTLibrary.Repository.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Event Publisher.
    /// </summary>
    public partial class EventPublisher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisher"/> class.
        /// </summary>
        public EventPublisher()
        {
            EventEntityTopic = new HashSet<EventEntityTopic>();
        }

        /// <summary>
        /// Gets or sets EventPublisherId.
        /// </summary>
        public short EventPublisherId { get; set; }

        /// <summary>
        /// Gets or sets EventPublisherName.
        /// </summary>
        public string EventPublisherName { get; set; }

        /// <summary>
        /// Gets or sets EventPublisherDescription.
        /// </summary>
        public string EventPublisherDescription { get; set; }

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
        /// Gets or sets EventEntityTopic.
        /// </summary>
        public ICollection<EventEntityTopic> EventEntityTopic { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays
#pragma warning restore CA2227 // Collection properties should be read only