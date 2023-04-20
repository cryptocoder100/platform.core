#pragma warning disable CA1024 // Use properties where appropriate
#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exos.Platform.AspNetCore.Security;
    using Exos.Platform.TenancyHelper.Interfaces;

    /// <inheritdoc/>
    public class UserHttpContextAccessorService : IUserHttpContextAccessorService
    {
        private readonly IUserContextService _userContextService;
        private readonly IUserContextAccessor _userContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserHttpContextAccessorService"/> class.
        /// </summary>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="userContextAccessor">IUserContextAccessor.</param>
        public UserHttpContextAccessorService(IUserContextService userContextService, IUserContextAccessor userContextAccessor)
        {
            _userContextService = userContextService;
            _userContextAccessor = userContextAccessor;
        }

        /// <inheritdoc/>
        public string GetCurrentUserId()
        {
            return _userContextAccessor.UserId;
        }

        /// <inheritdoc/>
        public string GetCurrentUser()
        {
            return _userContextService.Username;
        }

        /// <inheritdoc/>
        public long GetTenantId()
        {
            return _userContextService.TenantId;
        }

        /// <inheritdoc/>
        public List<long> GetServicerGroups()
        {
            return _userContextService.ServicerGroups;
        }

        /// <inheritdoc/>
        public string GetTenantType()
        {
            return _userContextService.TenantType;
        }

        /// <inheritdoc/>
        public List<long> GetSubClients()
        {
            return _userContextService.SubClientIds;
        }

        /// <inheritdoc/>
        public List<long> GetMasterClients()
        {
            return _userContextService.MasterClientIds;
        }

        /// <inheritdoc/>
        public List<long> GetSubVendors()
        {
            return _userContextService.SubVendorIds;
        }

        /// <inheritdoc/>
        public List<long> GetVendors()
        {
            return _userContextService.VendorIds;
        }

        /// <inheritdoc/>
        public IUserContext GetUserContext()
        {
            return _userContextService.GetUserContext();
        }

        /// <inheritdoc/>
        public string GetTrackingId()
        {
            return _userContextAccessor.TrackingId;
        }

        /// <inheritdoc/>
        public string GetClientKeyIdentifier()
        {
            string requestSubDomain = _userContextAccessor.RequestSubDomain;

            if (!string.IsNullOrEmpty(requestSubDomain))
            {
                // In some cases the subdomain (X-Forwarded-Host) has more than 1 value like  boadev-s3.exostechnology.com, gatewaysvc-s3-v1.dev.exostechnology.local
                string[] requestSubDomains = requestSubDomain.Split(",", StringSplitOptions.RemoveEmptyEntries);
                if (requestSubDomains != null && requestSubDomains.Any())
                {
                    requestSubDomain = requestSubDomains[0];
                }

                return requestSubDomain.Trim();
            }
            else
            {
                return null;
            }
        }
    }
}
#pragma warning restore CA1024 // Use properties where appropriate
#pragma warning restore CA1308 // Normalize strings to uppercase