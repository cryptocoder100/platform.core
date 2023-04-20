namespace Exos.Platform.Messaging.Repository.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Publish Error Message Log Entity.
    /// </summary>
    public class PublishErrorMessageLog
    {
        /// <summary>
        /// Gets or sets PublishErrorMessageLogId.
        /// </summary>
        public long PublishErrorMessageLogId { get; set; }

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
        /// Gets or sets MetaData.
        /// </summary>
        [StringLength(2000)]
        public string MetaData { get; set; }

        /// <summary>
        /// Gets or sets TransactionId.
        /// </summary>
        [StringLength(100)]
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets Publisher.
        /// </summary>
        [Required]
        [StringLength(250)]
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or sets Status.
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets MessageEntityName.
        /// </summary>
        [StringLength(100)]
        public string MessageEntityName { get; set; }

        /// <summary>
        /// Gets or sets FailedDateTime.
        /// </summary>
        public DateTime? FailedDateTime { get; set; }

        /// <summary>
        /// Gets or sets ReProcessDateTime.
        /// </summary>
        public DateTime? ReProcessDateTime { get; set; }

        /// <summary>
        /// Gets or sets RetryCount.
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// Gets or sets Comments.
        /// </summary>
        public string Comments { get; set; }

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
