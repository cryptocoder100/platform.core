#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Class to be inherit for the Entity that will be used for the table that store events.
    /// </summary>
    public class EventTrackingAzureTableEntity : TableEntity, ITableEntity
    {
        /// <summary>
        /// Gets or sets EventId.
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// Gets or sets TrackingId.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets Priority.
        /// </summary>
        public short Priority { get; set; }

        /// <summary>
        /// Gets or sets EventName.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets PublisherName.
        /// </summary>
        public string PublisherName { get; set; }

        /// <summary>
        /// Gets or sets PublisherId.
        /// </summary>
        public short PublisherId { get; set; }

        /// <summary>
        /// Gets or sets Payload.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Gets or sets Metadata.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets UserContext.
        /// </summary>
        public string UserContext { get; set; }

        /// <summary>
        /// Gets or sets PrimaryKeyValue.
        /// </summary>
        public string PrimaryKeyValue { get; set; }

        /// <summary>
        /// Gets or sets OldValue.
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// Gets or sets NewValue.
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether isActive.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets DueDate.
        /// </summary>
        public DateTime? DueDate { get; set; }

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
    }
}
#pragma warning restore CA1819 // Properties should not return arrays
