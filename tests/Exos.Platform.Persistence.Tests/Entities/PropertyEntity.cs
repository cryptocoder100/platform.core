#pragma warning disable CA1819 // Properties should not return arrays
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Exos.Platform.Persistence.Encryption;
using Exos.Platform.Persistence.Entities;
using Exos.Platform.TenancyHelper.Entities;

namespace Exos.Platform.Persistence.Tests.Entities
{
    /// <summary>
    /// Defines the <see cref="PropertyEntity" />.
    /// </summary>
    public class PropertyEntity : IAuditable, ITenant
    {
        /// <inheritdoc/>
        public string EntityName => "Property";

        /// <summary>
        /// Gets or sets the PropertyId.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long PropertyId { get; set; }

        /// <summary>
        /// Gets or sets the BorrowerId.
        /// </summary>
        public long BorrowerId { get; set; }

        /// <summary>
        /// Gets or sets the Address.
        /// </summary>
        [Encrypted]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the City.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the State.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the Zip.
        /// </summary>
        public string Zip { get; set; }

        /// <inheritdoc/>
        public bool IsActive { get; set; }

        /// <inheritdoc/>
        [Required]
        [Column(TypeName = "smalldatetime")]
        public DateTime CreatedDate { get; set; }

        /// <inheritdoc/>
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        /// <inheritdoc/>
        [Column(TypeName = "smalldatetime")]
        public DateTime? LastUpdatedDate { get; set; }

        /// <inheritdoc/>
        [StringLength(100)]
        public string LastUpdatedBy { get; set; }

        /// <inheritdoc/>
        [Timestamp]
        public byte[] Version { get; set; }

        /// <inheritdoc/>
        public int ClientTenantId { get; set; }

        /// <inheritdoc/>
        public int SubClientTenantId { get; set; }

        /// <inheritdoc/>
        public int VendorTenantId { get; set; }

        /// <inheritdoc/>
        public int SubContractorTenantId { get; set; }

        /// <inheritdoc/>
        public short ServicerTenantId { get; set; }

        /// <inheritdoc/>
        public short ServicerGroupTenantId { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays