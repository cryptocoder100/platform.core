namespace Exos.Platform.Persistence.EventTracking
{
    using System;

    /// <summary>
    ///  Explicit events, not persistent events.
    /// </summary>
    public class ExplicitEvent
    {
        /// <summary>
        /// Gets or sets PrimaryKeyValue.
        /// </summary>
        public string PrimaryKeyValue { get; set; } // Need to be able to track events by work order Id.

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
        /// Gets or sets DueDate.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets FutureEventCtx.
        /// </summary>
        public FutureEventCtx FutureEventCtx { get; set; }
    }
}
