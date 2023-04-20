#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// This should be used from Web api triggered from UI.
    /// </summary>
    public class UserContextAccessor : IUserContextAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextAccessor"/> class.
        /// </summary>
        /// <param name="context">IHttpContextAccessor.</param>
        /// <param name="logger">ILogger.</param>
        public UserContextAccessor(IHttpContextAccessor context, ILogger<UserContextAccessor> logger)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.LogDebug($"UserContextAccessor constructor called, created an instance with following identifier: {Guid.NewGuid()}");

            var identity = context.HttpContext?.User?.Identity;
            // Bug# 263091 - Performance-: Functional NPE - Exos.AspNetCore.Platform.Security.UserContextAccessor
            if (identity != null)
            {
                UserId = identity.GetUserId();
                Roles = identity.GetRoles();
                TenantId = identity.GetTenantId();
                TenantType = identity.GetTenantType();
                SubVendorIds = identity.GetSubcontractors();
                VendorIds = identity.GetVendors();
                SubClientIds = identity.GetSubClients();
                MasterClientIds = identity.GetMasterClients();
                LinesOfBusiness = identity.GetLinesOfBusiness();
                Username = identity.Name;

                // Validate if UserName is a GUID then we should use the userModel.FirstName instead
                // Guid is stored for integration users
                if (Guid.TryParse(Username, out _))
                {
                    Username = identity.GetGivenName();
                }

                AssociatedServicerTenantIds = identity.GetAssociatedServicerTenantIds();

                // Dedicated to work order tenancy and never cached
                ServicerTenantId = identity.GetServicerTenantId();
                ServicerGroupTenantId = identity.GetWorkorderServicerGroupTenantId();
                VendorTenantId = identity.GetWorkOrderVendorTenantId();
                SubContractorTenantId = identity.GetWorkOrderSubcontractorTenantId();
                ClientTenantId = identity.GetWorkOrderMasterTenantId();
                SubClientTenantId = identity.GetWorkOrderSubClientTenantId();
                HeaderWorkOrderId = identity.GetHeaderWorkOrderId();
                SourceSystemWorkOrderNumber = identity.GetSourceSystemWorkOrderNumber();
                SourceSystemOrderNumber = identity.GetSourceSystemOrderNumber();

                ServicerTenantFeatures = identity.GetServicerTenantFeatures();

                TrackingId = identity.GetTrackingId(context);
                ServicerGroups = identity.GetServicerGroups();
                ClaimWorkOrderIds = identity.GetWorkOrderIds();
                TeamIds = identity.GetTeamsIds();
                IsManager = identity.GetIsManager();

                OperationalType = identity.GetOperationalTypes();
                SubTenantType = identity.GetSubTenantType();

                // Set request domain.
                RequestSubDomain = context.HttpContext.GetRequestSubDomain();

                // Set the exos Admin
                ExosAdmin = identity.GetExosAdmin();
            }
            else
            {
                logger.LogWarning("User Identity is null");
                // TODO: we should throw an exception here if the user identity is null.
            }

            // Make deep copy of claims to be used by app.
            Claims = context.HttpContext?.CloneClaims();
        }

        /// <inheritdoc/>
        public string UserId { get; set; }

        /// <inheritdoc/>
        public string Username { get; set; }

        /// <inheritdoc/>
        public List<string> Roles { get; set; }

        /// <inheritdoc/>
        public string TenantType { get; set; }

        /// <inheritdoc/>
        public long TenantId { get; set; }

        /// <inheritdoc/>
        public List<long> AssociatedServicerTenantIds { get; set; }

        /// <inheritdoc/>
        public List<long> LinesOfBusiness { get; set; }

        /// <inheritdoc/>
        public List<string> BusinessFunctions { get; set; }

        /// <inheritdoc/>
        public List<long> VendorIds { get; set; }

        /// <inheritdoc/>
        public List<long> SubVendorIds { get; set; }

        /// <inheritdoc/>
        public List<long> SubClientIds { get; set; }

        /// <inheritdoc/>
        public List<long> MasterClientIds { get; set; }

        /// <inheritdoc/>
        public string Token { get; set; }

        /// <inheritdoc/>
        public string TrackingId { get; set; }

        /// <inheritdoc/>
        public List<long> ServicerGroups { get; set; }

        /// <inheritdoc/>
        public List<string> TeamIds { get; set; }

        /// <inheritdoc/>
        public long ServicerTenantId { get; set; }

        /// <inheritdoc/>
        public List<string> ServicerTenantFeatures { get; set; }

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
        public long HeaderWorkOrderId { get; set; }

        /// <inheritdoc/>
        public string SourceSystemWorkOrderNumber { get; set; }

        /// <inheritdoc/>
        public string SourceSystemOrderNumber { get; set; }

        /// <inheritdoc/>
        public bool? IsManager { get; set; }

        /// <inheritdoc/>
        public List<long> ClaimWorkOrderIds { get; set; }

        /// <inheritdoc/>
        public IEnumerable<Claim> Claims { get; set; }

        /// <inheritdoc/>
        public List<int> OperationalType { get; set; }

        /// <inheritdoc/>
        public string SubTenantType { get; set; }

        /// <inheritdoc/>
        public string RequestSubDomain { get; set; }

        /// <inheritdoc/>
        public bool? ExosAdmin { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only