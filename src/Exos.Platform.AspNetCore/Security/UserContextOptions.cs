#pragma warning disable SA1402 // FileMayOnlyContainASingleType

namespace Exos.Platform.AspNetCore.Security
{
    /// <summary>
    /// User Context Configuration Options.
    /// </summary>
    public class UserContextOptions
    {
        /// <summary>
        /// Gets or sets a regular expression pattern of URLs to skip processing.
        /// The default is to process all authenticated requests.
        /// </summary>
        public string IgnoreInputPattern { get; set; }

        /// <summary>
        /// Gets or sets the URL of the user service for building user context.
        /// </summary>
        public string UserSvc { get; set; }

        /// <summary>
        /// Gets or sets the URL of the vendor management service for building user context.
        /// </summary>
        public string VendorManagementSvc { get; set; }

        /// <summary>
        /// Gets or sets the URL of the client management service for building user context.
        /// </summary>
        public string ClientManagementSvc { get; set; }

        /// <summary>
        /// Gets or sets the URL of the order management service for building user context.
        /// </summary>
        public string OrderManagementSvc { get; set; }

        /// <summary>
        /// Gets or sets key for signing cached values.
        /// </summary>
        public string KeyVaultKeyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether work order tenant information can be used if found
        /// in the inbound "Exos-Work-Order-Tenant" header.
        /// </summary>
        public bool AcceptInboundWorkOrderTenantHeader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether work order tenant information should be included
        /// in the "Exos-Work-Order-Tenant" header of outbound requests.
        /// </summary>
        public bool IncludeOutboundWorkOrderTenantHeader { get; set; }

        /// <summary>
        /// Gets or sets the secret used in the "Exos-Work-Order-Tenant" header signature.
        /// The secret is Base64 encoded.
        /// </summary>
        public string WorkOrderTenantHeaderSigningSecret { get; set; }

        /// <summary>
        /// Gets or sets the lifetime in minutes used for caching user claims.  This should be greater than the access_token lifetime.
        /// </summary>
        public int CachedClaimsLifetimeInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time.  The cache will be cleared after not being accessed for this # of minutes.
        /// </summary>
        public int CachedClaimsSlidingExpirationInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the lifetime in minutes used for caching Servicer claims.
        /// </summary>
        public int CachedServicerClaimsLifetimeInMinutes { get; set; } = 120; // 2 Hours by default.

        /// <summary>
        /// Gets or sets the shared gateway key used to sign a JWT.
        /// </summary>
        public string JwtSigningKey { get; set; }

        /// <summary>
        /// Gets or sets the shared gateway issuer used to sign a JWT.
        /// </summary>
        public string JwtIssuer { get; set; }

        /// <summary>
        /// Gets or sets the shared gateway audience used to sign a JWT.
        /// </summary>
        public string JwtAudience { get; set; }

        /// <summary>
        /// Gets or sets the shared gateway token lifetime in minutes used to sign a JWT.
        /// </summary>
        public int JwtLifetimeInMinutes { get; set; }
    }

    /// <summary>
    /// Token Options Configuration.
    /// </summary>
    public class TokenOptions : UserContextOptions
    {
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType