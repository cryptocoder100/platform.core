namespace Exos.Platform.Messaging.Repository.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Failed Subscription Message Log Entity.
    /// </summary>
    public class FailedSubscriptionMessageLog
    {
        /// <summary>
        /// Gets or sets FailedSubscriptionMessageLogId.
        /// </summary>
        public long FailedSubscriptionMessageLogId { get; set; }

        /// <summary>
        /// Gets or sets SubscriptionName.
        /// </summary>
        [Required]
        [StringLength(250)]
        public string SubscriptionName { get; set; }

        /// <summary>
        /// Gets or sets ServiceBusEntityName.
        /// </summary>
        [StringLength(100)]
        public string ServiceBusEntityName { get; set; }

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
        [StringLength(100)]
        public string TransactionId { get; set; }

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
        /// Gets or sets Status.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets FailedDateTime.
        /// </summary>
        [Required]
        public DateTime FailedDateTime { get; set; }

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

        /// <summary>
        /// Gets or sets ErrorMessage.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets RetryCount.
        /// </summary>
        public short RetryCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsActive.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
