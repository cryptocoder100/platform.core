#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.TenancyHelper.Interfaces
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface that represent the User Context.
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        /// Gets or sets UserId.
        /// </summary>
        string UserId { get; set; }

        /// <summary>
        /// Gets or sets Username.
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// Gets or sets Roles.
        /// </summary>
        List<string> Roles { get; set; }

        /* ACL getAccessControlList();*/

        /// <summary>
        /// Gets or sets TenantType.
        /// </summary>
        string TenantType { get; set; }

        /// <summary>
        /// Gets or sets SubTenantType.
        /// </summary>
        string SubTenantType { get; set; }

        /// <summary>
        /// Gets or sets TenantId.
        /// </summary>
        long TenantId { get; set; }

        /// <summary>
        /// Gets or sets AssociatedServicerTenantIds.
        /// Exos admin can have more than one tenant associated with the account.
        /// VendorUser can also have multiple servicers tied to it.
        /// </summary>
        List<long> AssociatedServicerTenantIds { get; set; }

        /// <summary>
        /// Gets or sets LinesOfBusiness.
        /// </summary>
        List<long> LinesOfBusiness { get; set; }

        /// <summary>
        /// Gets or sets BusinessFunctions.
        /// </summary>
        List<string> BusinessFunctions { get; set; }

        /// <summary>
        /// Gets or sets VendorIds.
        /// </summary>
        List<long> VendorIds { get; set; }

        /// <summary>
        /// Gets or sets SubVendorIds.
        /// </summary>
        List<long> SubVendorIds { get; set; } // for Vendor Firm tenant type only

        /// <summary>
        /// Gets or sets SubClientIds.
        /// </summary>
        List<long> SubClientIds { get; set; } // for Master Client tenant type only

        /// <summary>
        /// Gets or sets MasterClientIds.
        /// </summary>
        List<long> MasterClientIds { get; set; }

        /// <summary>
        /// Gets or sets Token.
        /// </summary>
        string Token { get; set; }

        /// <summary>
        /// Gets or sets TrackingId.
        /// </summary>
        string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets ServicerGroups.
        /// </summary>
        List<long> ServicerGroups { get; set; }

        /// <summary>
        /// Gets or sets TeamIds.
        /// </summary>
        List<string> TeamIds { get; set; }

        /// <summary>
        /// Gets or sets ServicerTenantId
        /// Current servicer tenant in play. Important filter criteria For Vendor/Sub Type of user.
        /// </summary>
        long ServicerTenantId { get; set; }

        /// <summary>
        /// Gets or sets ServicerTenantFeatures
        /// Current servicer tenant in play. Important filter criteria For Vendor/Sub Type of user.
        /// </summary>
        List<string> ServicerTenantFeatures { get; set; }

        // Following section is dedicated to workorder tenancy.

        /// <summary>
        /// Gets or sets ServicerGroupTenantId.
        /// </summary>
        long ServicerGroupTenantId { get; set; }

        /// <summary>
        /// Gets or sets VendorTenantId.
        /// </summary>
        long VendorTenantId { get; set; }

        /// <summary>
        /// Gets or sets SubContractorTenantId.
        /// </summary>
        long SubContractorTenantId { get; set; }

        /// <summary>
        /// Gets or sets ClientTenantId.
        /// </summary>
        long ClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets SubClientTenantId.
        /// </summary>
        long SubClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets IsManager.
        /// </summary>
        bool? IsManager { get; set; }

        /// <summary>
        /// Gets or sets the ExosAdmin.
        /// </summary>
        bool? ExosAdmin { get; set; }

        // Following section is dedicated to Consumer kind of tenants where we don't have proper tenancy set on data.

        /// <summary>
        /// Gets or sets ClaimWorkOrderIds
        /// The list of work orders when consumer is logged in.
        /// </summary>
        List<long> ClaimWorkOrderIds { get; set; }

        /// <summary>
        /// Gets or sets OperationalType.
        /// </summary>
        List<int> OperationalType { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only

