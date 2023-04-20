namespace Exos.Platform.Messaging.Repository.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Message Log Entity.
    /// </summary>
    public class MessageLog
    {
        /// <summary>
        /// Gets or sets MessageLogId.
        /// </summary>
        public long MessageLogId { get; set; }

        /// <summary>
        /// Gets or sets MessageGuid.
        /// </summary>
        [Required]
        public Guid MessageGuid { get; set; }

        /// <summary>
        /// Gets or sets Payload.
        /// </summary>
        [Required]
        [StringLength(8000)]
        public string Payload { get; set; }

        /// <summary>
        /// Gets or sets TransactionId.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets ServiceBusEntityName.
        /// </summary>
        [StringLength(100)]
        public string ServiceBusEntityName { get; set; }

        /// <summary>
        /// Gets or sets MetaData.
        /// </summary>
        [StringLength(2000)]
        public string MetaData { get; set; }

        /// <summary>
        /// Gets or sets Publisher.
        /// </summary>
        [Required]
        [StringLength(250)]
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or sets ReceivedDateTime.
        /// </summary>
        [Required]
        public DateTime ReceivedDateTime { get; set; }

        /// <summary>
        /// Gets or sets CreatedById.
        /// </summary>
        [Required]
        public int CreatedById { get; set; }

        /// <summary>
        /// Gets or sets CreatedDate.
        /// </summary>
        [Required]
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedById.
        /// </summary>
        public int? LastUpdatedById { get; set; }

        /// <summary>
        /// Gets or sets LastUpdatedDate.
        /// </summary>
        public DateTime? LastUpdatedDate { get; set; }
    }
}
