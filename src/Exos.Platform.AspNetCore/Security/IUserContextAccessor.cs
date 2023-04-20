#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.Security
{
    using System.Collections.Generic;
    using System.Security.Claims;

    /// <summary>
    /// User Context Accessor.
    /// </summary>
    public interface IUserContextAccessor
    {
        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        string UserId { get; set; }

        /// <summary>
        /// Gets or sets the Username.
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// Gets or sets the Roles.
        /// </summary>
        List<string> Roles { get; set; }

        /// <summary>
        /// Gets or sets the TenantType.
        /// </summary>
        string TenantType { get; set; }

        /// <summary>
        /// Gets or sets the SubTenantType.
        /// </summary>
        string SubTenantType { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        long TenantId { get; set; }

        /// <summary>
        /// Gets or sets the AssociatedServicerTenantIds.
        /// Exos admin can have more than one tenant associated with the account.
        /// VendorUser can also have multiple servicers tied to it.
        /// </summary>
        List<long> AssociatedServicerTenantIds { get; set; }

        /// <summary>
        /// Gets or sets the LinesOfBusiness.
        /// </summary>
        List<long> LinesOfBusiness { get; set; }

        /// <summary>
        /// Gets or sets the BusinessFunctions.
        /// </summary>
        List<string> BusinessFunctions { get; set; }

        /// <summary>
        /// Gets or sets the VendorIds.
        /// </summary>
        List<long> VendorIds { get; set; }

        /// <summary>
        /// Gets or sets the SubVendorIds.
        /// </summary>
        List<long> SubVendorIds { get; set; }

        /// <summary>
        /// Gets or sets the SubClientIds.
        /// </summary>
        List<long> SubClientIds { get; set; }

        /// <summary>
        /// Gets or sets the MasterClientIds.
        /// </summary>
        List<long> MasterClientIds { get; set; }

        /// <summary>
        /// Gets or sets the Token.
        /// </summary>
        string Token { get; set; }

        /// <summary>
        /// Gets or sets the TrackingId.
        /// </summary>
        string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the ServicerGroups.
        /// </summary>
        List<long> ServicerGroups { get; set; }

        /// <summary>
        /// Gets or sets the TeamIds.
        /// </summary>
        List<string> TeamIds { get; set; }

        /// <summary>
        /// Gets or sets the ServicerTenantId.
        /// </summary>
        long ServicerTenantId { get; set; }

        /// <summary>
        /// Gets or sets the ServicerTenantFeatures.
        /// </summary>
        List<string> ServicerTenantFeatures { get; set; }

        /// <summary>
        /// Gets or sets the ServicerGroupTenantId.
        /// </summary>
        long ServicerGroupTenantId { get; set; }

        /// <summary>
        /// Gets or sets the VendorTenantId.
        /// </summary>
        long VendorTenantId { get; set; }

        /// <summary>
        /// Gets or sets the SubContractorTenantId.
        /// </summary>
        long SubContractorTenantId { get; set; }

        /// <summary>
        /// Gets or sets the ClientTenantId.
        /// </summary>
        long ClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets the SubClientTenantId.
        /// </summary>
        long SubClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets the HeaderWorkOrderId.
        /// </summary>
        long HeaderWorkOrderId { get; set; }

        /// <summary>
        /// Gets or sets the SourceSystemWorkOrderNumber.
        /// </summary>
        string SourceSystemWorkOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the SourceSystemOrderNumber.
        /// </summary>
        string SourceSystemOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the IsManager.
        /// </summary>
        bool? IsManager { get; set; }

        /// <summary>
        /// Gets or sets the ClaimWorkOrderIds.
        /// This is the list of work orders when consumer is logged in.
        /// </summary>
        List<long> ClaimWorkOrderIds { get; set; }

        /// <summary>
        /// Gets or sets the Claims.
        /// </summary>
        IEnumerable<Claim> Claims { get; set; }

        /// <summary>
        /// Gets or sets the OperationalType.
        /// </summary>
        List<int> OperationalType { get; set; }

        /// <summary>
        /// Gets or sets the request subdomain.
        /// like ui.exostechnology.com.
        /// </summary>
        string RequestSubDomain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is an ExosAdmin.
        /// </summary>
        bool? ExosAdmin { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only