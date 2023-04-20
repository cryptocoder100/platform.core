#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Class to be inherit for the Entity that will be used for the table that store events.
    /// </summary>
    public abstract class EventTrackingEntity
    {
        /// <summary>
        /// Gets or sets EventId.
        /// </summary>
        public abstract int EventId { get; set; }

        /// <summary>
        /// Gets or sets TrackingId.
        /// </summary>
        public abstract string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets Priority.
        /// </summary>
        public abstract short Priority { get; set; }

        /// <summary>
        /// Gets or sets EventName.
        /// </summary>
        public abstract string EventName { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        public abstract string EntityName { get; set; }

        /// <summary>
        /// Gets or sets PublisherName.
        /// </summary>
        public abstract string PublisherName { get; set; }

        /// <summary>
        /// Gets or sets PublisherId.
        /// </summary>
        public abstract short PublisherId { get; set; }

        /// <summary>
        /// Gets or sets Payload.
        /// </summary>
        public abstract string Payload { get; set; }

        /// <summary>
        /// Gets or sets Metadata.
        /// </summary>
        public abstract string Metadata { get; set; }

        /// <summary>
        /// Gets or sets UserContext.
        /// </summary>
        public abstract string UserContext { get; set; }

        /// <summary>
        /// Gets or sets PrimaryKeyValue.
        /// </summary>
        public abstract string PrimaryKeyValue { get; set; }

        /// <summary>
        /// Gets or sets OldValue.
        /// </summary>
        public abstract string OldValue { get; set; }

        /// <summary>
        /// Gets or sets NewValue.
        /// </summary>
        public abstract string NewValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether isActive.
        /// </summary>
        public abstract bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets DueDate.
        /// </summary>
        public abstract DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets CreatedDate.
        /// </summary>
        public abstract DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets CreatedBy.
        /// </summary>
        public abstract string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedDate.
        /// </summary>
        public abstract DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedBy.
        /// </summary>
        public abstract string LastUpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets Version.
        /// </summary>
        public abstract byte[] Version { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays
