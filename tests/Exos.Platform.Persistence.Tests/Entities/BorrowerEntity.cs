#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Persistence.Tests.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Exos.Platform.Persistence.Encryption;
    using Exos.Platform.Persistence.Entities;
    using Exos.Platform.TenancyHelper.Entities;

    /// <summary>
    /// Test Borrower Entity.
    /// </summary>
    public class BorrowerEntity : IAuditable, ITenant
    {
        /// <inheritdoc/>
        public string EntityName => "Borrower";

        /// <summary>
        /// Gets or sets BorrowerId.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long BorrowerId { get; set; }

        /// <summary>
        /// Gets or sets FirstName.
        /// </summary>
        [Encrypted]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets MiddleName.
        /// </summary>
        [Encrypted]
        public string MiddleName { get; set; }

        /// <summary>
        /// Gets or sets LastName.
        /// </summary>
        [Encrypted]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets SSN.
        /// </summary>
        [Encrypted]
        public string SSN { get; set; }

        /// <summary>
        /// Gets or sets DayPhone.
        /// </summary>
        [Encrypted]
        public string DayPhone { get; set; }

        /// <summary>
        /// Gets or sets EMail.
        /// </summary>
        [Encrypted]
        public string EMail { get; set; }

        /// <summary>
        /// Gets or sets EvenPhone.
        /// </summary>
        [Encrypted]
        public string EvenPhone { get; set; }

        /// <summary>
        /// Gets or sets Addr1.
        /// </summary>
        public string Addr1 { get; set; }

        /// <summary>
        /// Gets or sets City.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets State.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets Zip.
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