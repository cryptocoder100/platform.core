#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.ICTLibrary.Repository.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Event Tracking Entity.
    /// </summary>
    public partial class EventTracking
    {
        /// <summary>
        /// Gets or sets EventTrackingId.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventTrackingId { get; set; }

        /// <summary>
        /// Gets or sets TrackingId.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets EventName.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets ApplicationName.
        /// </summary>
        public string ApplicationName { get; set; }

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
    }
}
#pragma warning restore CA1819 // Properties should not return arrays