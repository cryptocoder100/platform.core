namespace Exos.Platform.Persistence.Persistence.EventPoller
{
    using System;
    using Exos.Platform.Persistence.EventTracking;

    /// <summary>
    /// The integration's event queue model.
    /// </summary>
    /// <seealso cref="Exos.Platform.Persistence.EventTracking.EventTrackingEntity" />
    public class IntegrationsEventQueueModel : EventTrackingEntity
    {
        /// <summary>
        /// Gets or sets EventId.
        /// </summary>
        public override int EventId { get; set; }

        /// <summary>
        /// Gets or sets TrackingId.
        /// </summary>
        public override string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets Priority.
        /// </summary>
        public override short Priority { get; set; }

        /// <summary>
        /// Gets or sets EventName.
        /// </summary>
        public override string EventName { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        public override string EntityName { get; set; }

        /// <summary>
        /// Gets or sets PublisherName.
        /// </summary>
        public override string PublisherName { get; set; }

        /// <summary>
        /// Gets or sets PublisherId.
        /// </summary>
        public override short PublisherId { get; set; }

        /// <summary>
        /// Gets or sets Payload.
        /// </summary>
        public override string Payload { get; set; }

        /// <summary>
        /// Gets or sets Metadata.
        /// </summary>
        public override string Metadata { get; set; }

        /// <summary>
        /// Gets or sets UserContext.
        /// </summary>
        public override string UserContext { get; set; }

        /// <summary>
        /// Gets or sets PrimaryKeyValue.
        /// </summary>
        public override string PrimaryKeyValue { get; set; }

        /// <summary>
        /// Gets or sets OldValue.
        /// </summary>
        public override string OldValue { get; set; }

        /// <summary>
        /// Gets or sets new value.
        /// </summary>
        public override string NewValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether isActive.
        /// </summary>
        public override bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets DueDate.
        /// </summary>
        public override DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets CreatedDate.
        /// </summary>
        public override DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets CreatedBy.
        /// </summary>
        public override string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedDate.
        /// </summary>
        public override DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedBy.
        /// </summary>
        public override string LastUpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the source service.
        /// </summary>
        public string SourceService { get; set; }

        /// <summary>
        /// Gets or sets Version.
        /// </summary>
        public override byte[] Version { get; set; }
    }
}
