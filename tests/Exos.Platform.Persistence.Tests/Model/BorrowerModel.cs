#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Persistence.Tests.Model
{
    using System;
    using Exos.Platform.Persistence.Encryption;

    /// <summary>
    /// Defines the <see cref="BorrowerModel" />.
    /// </summary>
    public class BorrowerModel
    {
        /// <summary>
        /// Gets or sets the BorrowerId.
        /// </summary>
        public long BorrowerId { get; set; }

        /// <summary>
        /// Gets or sets the FirstName.
        /// </summary>
        [Encrypted]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the MiddleName.
        /// </summary>
        [Encrypted]
        public string MiddleName { get; set; }

        /// <summary>
        /// Gets or sets the LastName.
        /// </summary>
        [Encrypted]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the SSN.
        /// </summary>
        [Encrypted]
        public string SSN { get; set; }

        /// <summary>
        /// Gets or sets the DayPhone.
        /// </summary>
        [Encrypted]
        public string DayPhone { get; set; }

        /// <summary>
        /// Gets or sets the EMail.
        /// </summary>
        [Encrypted]
        public string EMail { get; set; }

        /// <summary>
        /// Gets or sets the EvenPhone.
        /// </summary>
        [Encrypted]
        public string EvenPhone { get; set; }

        /// <summary>
        /// Gets or sets the Addr1.
        /// </summary>
        public string Addr1 { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether IsActive.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the CreatedDate.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the CreatedBy.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the LastUpdatedDate.
        /// </summary>
        public DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the LastUpdatedBy.
        /// </summary>
        public string LastUpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the Version.
        /// </summary>
        public byte[] Version { get; set; }

        /// <summary>
        /// Gets or sets the ClientTenantId.
        /// </summary>
        public int ClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets the SubClientTenantId.
        /// </summary>
        public int SubClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets the VendorTenantId.
        /// </summary>
        public int VendorTenantId { get; set; }

        /// <summary>
        /// Gets or sets the SubContractorTenantId.
        /// </summary>
        public int SubContractorTenantId { get; set; }

        /// <summary>
        /// Gets or sets the ServicerTenantId.
        /// </summary>
        public short ServicerTenantId { get; set; }

        /// <summary>
        /// Gets or sets the ServicerGroupTenantId.
        /// </summary>
        public short ServicerGroupTenantId { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays