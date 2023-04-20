#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Messaging.Repository.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Message Entity.
    /// </summary>
    public class MessageEntity
    {
        /// <summary>
        /// Gets or sets MessageEntityId.
        /// </summary>
        public long MessageEntityId { get; set; }

        /// <summary>
        /// Gets or sets NameSpace.
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string NameSpace { get; set; }

        /// <summary>
        /// Gets or sets PassiveNameSpace.
        /// </summary>
        [StringLength(2000)]
        public string PassiveNameSpace { get; set; }

        /// <summary>
        /// Gets or sets ConnectionString.
        /// </summary>
        [StringLength(2000)]
        [NotMapped]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets PassiveConnectionString.
        /// </summary>
        [StringLength(2000)]
        [NotMapped]
        public string PassiveConnectionString { get; set; }

        /// <summary>
        /// Gets or sets EventHubConnectionString.
        /// </summary>
        [StringLength(2000)]
        [NotMapped]
        public string EventHubConnectionString { get; set; }

        /// <summary>
        /// Gets or sets EventHubEndpoint.
        /// </summary>
        [StringLength(250)]
        [NotMapped]
        public string EventHubEndpoint { get; set; }

        /// <summary>
        /// Gets or sets MaxRetryCount.
        /// </summary>
        public int MaxRetryCount { get; set; }

        /// <summary>
        /// Gets or sets ServiceBusEntityType.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ServiceBusEntityType { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets MessageContext.
        /// </summary>
        [StringLength(1000)]
        public string MessageContext { get; set; }

        /// <summary>
        /// Gets or sets Owner.
        /// </summary>
        [StringLength(250)]
        public string Owner { get; set; }

        /// <summary>
        /// Gets or sets Status.
        /// </summary>
        [StringLength(250)]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets Comments.
        /// </summary>
        [StringLength(2000)]
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

        /// <summary>
        /// Gets or sets Version field, use for concurrency check.
        /// </summary>
        [Column(TypeName = "timestamp")]
        [MaxLength(8)]
        [Timestamp]
        public byte[] Version { get; set; }

        /// <summary>
        /// Gets or sets IsPublishToServiceBusActive.
        /// </summary>
        public bool? IsPublishToServiceBusActive { get; set; }

        /// <summary>
        /// Gets or sets EventHubNameSpace.
        /// </summary>
        [StringLength(2000)]
        public string EventHubNameSpace { get; set; }

        /// <summary>
        /// Gets or sets IsPublishToEventHubActive.
        /// </summary>
        public bool? IsPublishToEventHubActive { get; set; }

        /// <summary>
        /// Checks if the Service Bus connection string contains a SharedAccessKey.
        /// </summary>
        /// <param name="conn">The connection string to check.</param>
        /// <returns>true if SharedAccessKey is present.</returns>
        public static bool ConnectionStringHasKeys(string conn)
        {
            if (string.IsNullOrEmpty(conn))
            {
                throw new ArgumentNullException(nameof(conn), "No valid connection string was passed.");
            }

            return conn.ToUpperInvariant().Contains("SHAREDACCESSKEY", StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays