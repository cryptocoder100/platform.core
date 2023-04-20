#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;

    /// <summary>
    /// this should be used from background processes like web jobs OR where Http Context is not available.
    /// This specific implementation just returns null username and that by passes tenancy.
    /// </summary>
    public class BackgroundProcessUserContextAccessor : IUserContextAccessor
    {
        /// <inheritdoc/>
        public string UserId { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string Username { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<string> Roles { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string TenantType { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long TenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> AssociatedServicerTenantIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> LinesOfBusiness { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<string> BusinessFunctions { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> VendorIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> SubVendorIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> SubClientIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> MasterClientIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string Token { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string TrackingId { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> ServicerGroups { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<string> TeamIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long ServicerTenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<string> ServicerTenantFeatures { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long ServicerGroupTenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long VendorTenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long SubContractorTenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long ClientTenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long SubClientTenantId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public bool? IsManager { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<long> ClaimWorkOrderIds { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public IEnumerable<Claim> Claims { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public long HeaderWorkOrderId { get => default(long); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string SourceSystemWorkOrderNumber { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string SourceSystemOrderNumber { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public List<int> OperationalType { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string SubTenantType { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public string RequestSubDomain { get => null; set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public bool? ExosAdmin { get => null; set => throw new NotImplementedException(); }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only