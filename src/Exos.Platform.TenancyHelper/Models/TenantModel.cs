#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.TenancyHelper.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Tenant Model, hold Tenancy access conditions.
    /// </summary>
    public class TenantModel
    {
        /// <summary>
        /// Gets or sets the MasterClient.
        /// </summary>
        public long MasterClient { get; set; }

        /// <summary>
        /// Gets or sets the SubClient.
        /// </summary>
        public long SubClient { get; set; }

        /// <summary>
        /// Gets or sets the Vendor.
        /// </summary>
        public long Vendor { get; set; }

        /// <summary>
        /// Gets or sets the SubContractor.
        /// </summary>
        public long SubContractor { get; set; }

        /// <summary>
        ///  Gets or sets the ServicerGroup.
        /// </summary>
        public long ServicerGroup { get; set; }

        /// <summary>
        /// Gets or sets the ServicerIds.
        /// </summary>
        public List<long> ServicerIds { get; set; }

        /// <summary>
        /// Gets or sets the LineofBusinessid.
        /// </summary>
        public List<int> LineofBusinessid { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
