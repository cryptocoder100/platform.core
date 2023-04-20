#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Persistence.EventTracking
{
    using System;

    /// <summary>
    /// Event Publish Check Point Entity.
    /// </summary>
    public abstract class EventPublishCheckPointEntity
    {
        /// <summary>
        /// Gets or sets EventPublishCheckPointId.
        /// </summary>
        public abstract int EventPublishCheckPointId { get; set; }

        /// <summary>
        /// Gets or sets ProcessId.
        /// </summary>
        public abstract byte ProcessId { get; set; }

        /// <summary>
        /// Gets or sets EventId.
        /// </summary>
        public abstract int EventId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsActive.
        /// </summary>
        public abstract bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets CreatedDate.
        /// </summary>
        public abstract DateTime CreatedDate { get; set; }

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