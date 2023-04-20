#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.Persistence
{
    using System.Collections.Generic;

    /// <summary>
    /// Class to set the Tenancy values for the user.
    /// </summary>
    public class TenantValue
    {
        /// <summary>
        /// Gets or sets UserName.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets TenantType.
        /// </summary>
        public string TenantType { get; set; }

        /// <summary>
        /// Gets or sets TenantId.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets ServicerTenantId.
        /// </summary>
        public short ServicerTenantId { get; set; }

        /// <summary>
        /// Gets or sets MasterClientIds.
        /// </summary>
        public List<int> MasterClientIds { get; set; }

        /// <summary>
        /// Gets or sets SubClientIds.
        /// </summary>
        public List<int> SubClientIds { get; set; }

        /// <summary>
        /// Gets or sets VendorIds.
        /// </summary>
        public List<int> VendorIds { get; set; }

        /// <summary>
        /// Gets or sets SubcontractorIds.
        /// </summary>
        public List<int> SubcontractorIds { get; set; }

        /// <summary>
        /// Gets or sets ServicerGroupIds.
        /// </summary>
        public List<short> ServicerGroupIds { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only