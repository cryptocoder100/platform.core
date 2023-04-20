namespace Exos.Platform.Persistence
{
    using System.Collections.Generic;
    using Exos.Platform.TenancyHelper.Interfaces;

    /// <summary>
    /// Class to access HTTP Context.
    /// </summary>
    public interface IUserHttpContextAccessorService
    {
        /// <summary>
        /// Gets the current User Id.
        /// </summary>
        /// <returns>Returns the user id.</returns>
        string GetCurrentUserId();

        /// <summary>
        /// Gets the current User Name.
        /// </summary>
        /// <returns>Return the user name.</returns>
        string GetCurrentUser();

        /// <summary>
        /// Gets the list of Master Clients.
        /// </summary>
        /// <returns>List of Master Clients.</returns>
        List<long> GetMasterClients();

        /// <summary>
        /// Gets the list of Servicer Groups.
        /// </summary>
        /// <returns>List of Servicer Groups.</returns>
        List<long> GetServicerGroups();

        /// <summary>
        /// Gets the list of SubClients.
        /// </summary>
        /// <returns>List of SubClients.</returns>
        List<long> GetSubClients();

        /// <summary>
        /// Gets the list of SubVendors.
        /// </summary>
        /// <returns>List of SubVendors.</returns>
        List<long> GetSubVendors();

        /// <summary>
        /// Gets the Tenant Id.
        /// </summary>
        /// <returns>Tenant Id.</returns>
        long GetTenantId();

        /// <summary>
        /// Gets the Tenant Type.
        /// </summary>
        /// <returns>Tenant Type.</returns>
        string GetTenantType();

        /// <summary>
        /// Gets the list of Vendors.
        /// </summary>
        /// <returns>List of Vendors.</returns>
        List<long> GetVendors();

        /// <summary>
        /// Gets the User Context.
        /// </summary>
        /// <returns>User Context.</returns>
        IUserContext GetUserContext();

        /// <summary>
        /// Gets the Request Tracking Id.
        /// </summary>
        /// <returns>Request Tracking Id.</returns>
        string GetTrackingId();

        /// <summary>
        /// Gets the Client Key Identifier.
        /// </summary>
        /// <returns>Client name used to lookup the client encryption key.</returns>
        public string GetClientKeyIdentifier();
    }
}