#pragma warning disable SA1124 // DoNotUseRegions
namespace Exos.Platform.TenancyHelper.MultiTenancy
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Exos.Platform.AspNetCore.Security;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.Models;

    /// <inheritdoc/>
    public class UserContextService : IUserContextService
    {
        private readonly IUserContextAccessor _userContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextService"/> class.
        /// </summary>
        /// <param name="userContextAccessor">IUserContextAccessor.</param>
        public UserContextService(IUserContextAccessor userContextAccessor)
        {
            _userContextAccessor = userContextAccessor;
        }

        /// <summary>
        /// Gets Claims.
        /// </summary>
        public IEnumerable<Claim> Claims
        {
            get
            {
                return _userContextAccessor.Claims;
            }
        }

        /// <inheritdoc/>
        public string UserId
        {
            get
            {
                return _userContextAccessor.UserId;
            }
        }

        /// <inheritdoc/>
        public List<string> TeamIds
        {
            get
            {
                return _userContextAccessor.TeamIds;
            }
        }

        /// <inheritdoc/>
        public string Username
        {
            get
            {
                return _userContextAccessor.Username;
            }
        }

        /// <inheritdoc/>
        public List<string> Roles
        {
            get
            {
                return _userContextAccessor.Roles;
            }
        }

        /// <inheritdoc/>
        public string TenantType
        {
            get { return _userContextAccessor.TenantType; }
        }

        /// <inheritdoc/>
        public string SubTenantType
        {
            get { return _userContextAccessor.SubTenantType; }
        }

        /// <inheritdoc/>
        public long TenantId
        {
            get { return _userContextAccessor.TenantId; }
        }

        /// <inheritdoc/>
        public List<long> AssociatedServicerTenantIds
        {
            // Exos admins can have more than one tenant associated with the account.
            // VendorUser can also have multiple servicers tied to it.
            get { return _userContextAccessor.AssociatedServicerTenantIds; }
        }

        /// <inheritdoc/>
        public List<long> LinesOfBusiness
        {
            get { return _userContextAccessor.LinesOfBusiness; }
        }

        /// <inheritdoc/>
        public List<long> VendorIds
        {
            get { return _userContextAccessor.VendorIds; }
        }

        /// <inheritdoc/>
        public List<long> SubVendorIds
        {
            get { return _userContextAccessor.SubVendorIds; }
        }

        // Dedicated to work order tenancy and never cached

        /// <inheritdoc/>
        public long ServicerTenantId
        {
            get { return _userContextAccessor.ServicerTenantId; }
        }

        /// <summary>
        /// Gets ServicerGroupTenantId.
        /// </summary>
        public long? ServicerGroupTenantId
        {
            get { return _userContextAccessor.ServicerGroupTenantId; }
        }

        /// <summary>
        /// Gets VendorTenantId.
        /// </summary>
        public long? VendorTenantId
        {
            get { return _userContextAccessor.VendorTenantId; }
        }

        /// <summary>
        /// Gets SubContractorTenantId.
        /// </summary>
        public long? SubContractorTenantId
        {
            get { return _userContextAccessor.SubContractorTenantId; }
        }

        /// <summary>
        /// Gets ClientTenantId.
        /// </summary>
        public long? ClientTenantId
        {
            get { return _userContextAccessor.ClientTenantId; }
        }

        /// <summary>
        /// Gets SubClientTenantId.
        /// </summary>
        public long? SubClientTenantId
        {
            get { return _userContextAccessor.SubClientTenantId; }
        }

        /// <inheritdoc/>
        public List<long> SubClientIds
        {
            get { return _userContextAccessor.SubClientIds; }
        }

        /// <inheritdoc/>
        public List<long> MasterClientIds
        {
            get { return _userContextAccessor.MasterClientIds; }
        }

        /// <inheritdoc/>
        public List<string> ServicerFeatures
        {
            get { return _userContextAccessor.ServicerTenantFeatures; }
        }

        /// <summary>
        /// Gets TrackingId.
        /// </summary>
        public string TrackingId
        {
            get { return _userContextAccessor.TrackingId; }
        }

        /// <inheritdoc/>
        public List<long> ServicerGroups
        {
            get { return _userContextAccessor.ServicerGroups; }
        }

        /// <inheritdoc/>
        public List<long> ClaimWorkOrderIds
        {
            // This is the list work order ids for consumer kind of users.
            // This is needed for data authorization because consumers does not have user accounts in exos.
            get { return _userContextAccessor.ClaimWorkOrderIds; }
        }

        /// <inheritdoc/>
        public bool? IsManager
        {
            get { return _userContextAccessor.IsManager; }
        }

        /// <inheritdoc/>
        public List<int> OperationalType
        {
            get { return _userContextAccessor.OperationalType; }
        }

        /// <inheritdoc/>
        public bool? ExosAdmin
        {
            get { return _userContextAccessor.ExosAdmin; }
        }

        /// <inheritdoc/>
        public IUserContext GetUserContext()
        {
            IUserContext userContext = new UserContext
            {
                UserId = _userContextAccessor.UserId,
                Roles = _userContextAccessor.Roles,
                TenantId = _userContextAccessor.TenantId,
                TenantType = _userContextAccessor.TenantType,
                SubVendorIds = _userContextAccessor.SubVendorIds,
                VendorIds = _userContextAccessor.VendorIds,
                SubClientIds = _userContextAccessor.SubClientIds,
                MasterClientIds = _userContextAccessor.MasterClientIds,
            };
            userContext.Roles = _userContextAccessor.Roles;
            userContext.LinesOfBusiness = _userContextAccessor.LinesOfBusiness;
            userContext.Username = _userContextAccessor.Username;
            userContext.AssociatedServicerTenantIds = _userContextAccessor.AssociatedServicerTenantIds;

            // Region - Dedicated to work order tenancy and never cached
            userContext.ServicerTenantId = _userContextAccessor.ServicerTenantId;
            userContext.ServicerGroupTenantId = _userContextAccessor.ServicerGroupTenantId;
            userContext.VendorTenantId = _userContextAccessor.VendorTenantId;
            userContext.SubContractorTenantId = _userContextAccessor.SubContractorTenantId;
            userContext.ClientTenantId = _userContextAccessor.ClientTenantId;
            userContext.SubClientTenantId = _userContextAccessor.SubClientTenantId;

            // End Region -  Dedicated to work order tenancy and never cached
            userContext.ServicerTenantFeatures = _userContextAccessor.ServicerTenantFeatures;

            // ctx.Token = _userContextAccessor.Token(_options, _context);
            userContext.TrackingId = _userContextAccessor.TrackingId;
            userContext.ServicerGroups = _userContextAccessor.ServicerGroups;
            userContext.ClaimWorkOrderIds = _userContextAccessor.ClaimWorkOrderIds;
            userContext.TeamIds = _userContextAccessor.TeamIds;
            userContext.IsManager = _userContextAccessor.IsManager;
            userContext.ExosAdmin = _userContextAccessor.ExosAdmin;
            userContext.OperationalType = _userContextAccessor.OperationalType;
            userContext.SubTenantType = _userContextAccessor.SubTenantType;
            return userContext;
        }

        /// <inheritdoc/>
        public AuditModel GetAuditModel()
        {
            return new AuditModel()
            {
                CreatedBy = UserId,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false,
                LastUpdatedBy = UserId,
                LastUpdatedDate = DateTime.UtcNow,
            };
        }
    }
}
#pragma warning restore SA1124 // DoNotUseRegions
