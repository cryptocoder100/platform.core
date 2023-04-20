namespace Exos.Platform.Persistence.Tests.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Exos.Platform.Persistence.EventTracking;

    /// <inheritdoc/>
    public class EventQueueEntity : EventTrackingEntity
    {
        /// <inheritdoc/>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public override int EventId { get; set; }

        /// <inheritdoc/>
        public override string TrackingId { get; set; }

        /// <inheritdoc/>
        public override short Priority { get; set; }

        /// <inheritdoc/>
        public override string EventName { get; set; }

        /// <inheritdoc/>
        public override string EntityName { get; set; } = "EventQueueEntity";

        /// <inheritdoc/>
        public override string PublisherName { get; set; } = "Exos.Platform.Persistence.Tests";

        /// <inheritdoc/>
        public override short PublisherId { get; set; }

        /// <inheritdoc/>
        public override string Payload { get; set; }

        /// <inheritdoc/>
        public override string Metadata { get; set; }

        /// <inheritdoc/>
        public override string UserContext { get; set; }

        /// <inheritdoc/>
        public override string PrimaryKeyValue { get; set; }

        /// <inheritdoc/>
        public override string OldValue { get; set; }

        /// <inheritdoc/>
        public override string NewValue { get; set; }

        /// <inheritdoc/>
        public override bool IsActive { get; set; }

        /// <inheritdoc/>
        public override DateTime? DueDate { get; set; }

        /// <inheritdoc/>
        [Required]
        [StringLength(100)]
        public override string CreatedBy { get; set; }

        /// <inheritdoc/>
        [Required]
        [Column(TypeName = "smalldatetime")]
        public override DateTime? CreatedDate { get; set; }

        /// <inheritdoc/>
        [StringLength(100)]
        public override string LastUpdatedBy { get; set; }

        /// <inheritdoc/>
        [Column(TypeName = "smalldatetime")]
        public override DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the ClientTenantId.
        /// </summary>
        public int ClientTenantId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the SubClientTenantId.
        /// </summary>
        public int SubClientTenantId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the VendorTenantId.
        /// </summary>
        public int VendorTenantId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the SubContractorTenantId.
        /// </summary>
        public int SubContractorTenantId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the ServicerTenantId.
        /// </summary>
        public short ServicerTenantId { get; set; } = -1;

        /// <summary>
        /// Gets or sets the ServicerGroupTenantId.
        /// </summary>
        public short ServicerGroupTenantId { get; set; } = -1;

        /// <inheritdoc/>
        [Timestamp]
        public override byte[] Version { get; set; }
    }
}
