#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.TenancyHelper.MultiTenancy
{
    using System;
    using System.Collections.Generic;
    using Exos.Platform.TenancyHelper.Interfaces;

    /// <inheritdoc/>
    public class UserContext : IUserContext
    {
        // Need to be replaced with real User context.

        /// <inheritdoc/>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets BusinessFunctions.
        /// </summary>
        public List<long> BusinessFunctions { get; set; }

        /// <inheritdoc/>
        public List<long> LinesOfBusiness { get; set; }

        /// <inheritdoc/>
        public List<string> Roles { get; set; }

        /// <inheritdoc/>
        public List<long> MasterClientIds { get; set; }

        /// <inheritdoc/>
        public List<long> SubClientIds { get; set; }

        /// <inheritdoc/>
        public List<long> VendorIds { get; set; }

        /// <inheritdoc/>
        public List<long> SubVendorIds { get; set; }

        /// <inheritdoc/>
        public long TenantId { get; set; }

        /// <inheritdoc/>
        // Exos admin can have more than one tenant associated with the account. VendorUser can also have multiple servicer tied to it.
        public List<long> AssociatedServicerTenantIds { get; set; }

        /// <inheritdoc/>
        public string TenantType { get; set; }

        /// <inheritdoc/>
        public string Username { get; set; }

        /// <inheritdoc/>
        public long ServicerTenantId { get; set; } // current servicer tenant in play. Important filter criteria For Vendor/Sub Type of user.

        /// <inheritdoc/>
        public List<string> ServicerTenantFeatures { get; set; } // current servicer features.

        /// <inheritdoc/>
        List<string> IUserContext.BusinessFunctions { get; set; }

        /// <inheritdoc/>
        public string Token { get; set; }

        /// <inheritdoc/>
        public string TrackingId { get; set; }

        /// <inheritdoc/>
        public List<long> ServicerGroups { get; set; }

        /// <inheritdoc/>
        public long ServicerGroupTenantId { get; set; }

        /// <inheritdoc/>
        public long VendorTenantId { get; set; }

        /// <inheritdoc/>
        public long SubContractorTenantId { get; set; }

        /// <inheritdoc/>
        public long ClientTenantId { get; set; }

        /// <inheritdoc/>
        public long SubClientTenantId { get; set; }

        /// <inheritdoc/>
        public List<long> ClaimWorkOrderIds { get; set; }

        /// <inheritdoc/>
        public bool? IsManager { get; set; }

        /// <inheritdoc/>
        public bool? ExosAdmin { get; set; }

        /// <inheritdoc/>
        public List<string> TeamIds { get; set; }

        /// <inheritdoc/>
        public List<int> OperationalType { get; set; }

        /// <inheritdoc/>
        public string SubTenantType { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
