namespace Exos.Platform.TenancyHelper.Interfaces
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using Exos.Platform.TenancyHelper.Models;

    /// <summary>
    /// Interface to access the User Context.
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>
        /// Gets the LinesOfBusiness.
        /// </summary>
        List<long> LinesOfBusiness { get; }

        /// <summary>
        /// Gets the MasterClientIds.
        /// </summary>
        List<long> MasterClientIds { get; }

        /// <summary>
        /// Gets the Roles.
        /// </summary>
        List<string> Roles { get; }

        /// <summary>
        /// Gets the SubClientIds.
        /// </summary>
        List<long> SubClientIds { get; }

        /// <summary>
        /// Gets the SubVendorIds.
        /// </summary>
        List<long> SubVendorIds { get; }

        /// <summary>
        /// Gets the TenantId.
        /// </summary>
        long TenantId { get; } // Exos admin can have more than one tenant associated with the account. VendorUser can also have multiple servicers tied to it.

        /// <summary>
        /// Gets the AssociatedServicerTenantIds.
        /// </summary>
        List<long> AssociatedServicerTenantIds { get; }

        /// <summary>
        /// Gets the ServicerTenantId.
        /// Current servicer tenant in play. Important filter criteria For Vendor/Sub Type of user.
        /// </summary>
        long ServicerTenantId { get; }

        /// <summary>
        /// Gets the TenantType.
        /// </summary>
        string TenantType { get; }

        /// <summary>
        /// Gets the SubTenantType.
        /// </summary>
        string SubTenantType { get; }

        /// <summary>
        /// Gets the UserId.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets the TeamIds.
        /// </summary>
        List<string> TeamIds { get; }

        /// <summary>
        /// Gets the Username.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Gets the VendorIds.
        /// </summary>
        List<long> VendorIds { get; }

        /// <summary>
        /// Gets the ServicerFeatures.
        /// Current servicer features.
        /// </summary>
        List<string> ServicerFeatures { get; }

        /// <summary>
        /// Gets the ServicerGroups.
        /// </summary>
        List<long> ServicerGroups { get; }

        /// <summary>
        /// Gets the ClaimWorkOrderIds.
        /// Consumer Tenancy, work orders accessible for consumer.
        /// </summary>
        List<long> ClaimWorkOrderIds { get; }

        /// <summary>
        /// Gets the IsManager.
        /// </summary>
        bool? IsManager { get; }

        /// <summary>
        /// Gets the Claims
        /// All user claims.
        /// </summary>
        IEnumerable<Claim> Claims { get; }

        /// <summary>
        /// Gets the  OperationalType.
        /// </summary>
        List<int> OperationalType { get; }

        /// <summary>
        /// Gets the ExosAdmin.
        /// </summary>
        bool? ExosAdmin { get; }

        /// <summary>
        /// Gets the UserContext.
        /// </summary>
        /// <returns>User Context.</returns>
        IUserContext GetUserContext();

        /// <summary>
        /// Get the Audit Model.
        /// </summary>
        /// <returns>AuditModel.</returns>
        AuditModel GetAuditModel();
    }
}