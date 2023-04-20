using System;

namespace Exos.Platform.Messaging.Core.Listener
{
    /// <summary>
    /// Message Properties.
    /// </summary>
    public class MessageProperty
    {
        /// <summary>
        /// Gets or sets the MessageId.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Gets or sets the Message Label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the Message CorrelationId.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the Message MetaData.
        /// </summary>
        public string MetaDataProperties { get; set; }

        /// <summary>
        /// Gets or sets the logical sequence number of the event within the partition stream of
        ///     the Event Hub.
        /// </summary>
        public long? SequenceNumber { get; set; }

        /// <summary>
        ///     Gets or sets the date and time of the sent time in UTC.
        ///     The enqueue time in UTC.This value represents the actual time of enqueuing the
        ///     message.
        /// </summary>
        public DateTime? EnqueuedTimeUtc { get; set; }

        /// <summary>
        ///     Gets or sets the offset of the data relative to the Event Hub partition stream. The offset
        ///     is a marker or identifier for an event within the Event Hubs stream. The identifier
        ///     is unique within a partition of the Event Hubs stream.
        /// </summary>
        public string Offset { get; set; }
    }
}
