#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.AspNetCore.Extensions
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using Exos.Platform.AspNetCore.Encryption;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Extension methods for the HttpContext class.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Returns the value from the HTTP Request Header Tracking-Id.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Value from the HTTP Request Header Tracking-Id.</returns>
        public static string GetTrackingId(this HttpContext context)
        {
            return context?.Request?.GetTrackingId();
        }

        /// <summary>
        /// Returns the value from the HTTP Request Header servicertenantidentifier (Servicer Tenant Id).
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Value from the HTTP Request Header servicertenantidentifier.</returns>
        public static long? GetServicerContextTenantId(this HttpContext context)
        {
            return context?.Request?.GetServicerContextTenantId();
        }

        /// <summary>
        /// Returns the value from the HTTP Request Header workorderid (Work Order Id.).
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Value from the HTTP Request Header workorderid.</returns>
        public static long? GetWorkOrderId(this HttpContext context)
        {
            return context?.Request?.GetWorkOrderId();
        }

        /// <summary>
        /// Copies important properties of user claims.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>List of claims.</returns>
        public static IEnumerable<Claim> CloneClaims(this HttpContext context)
        {
            List<Claim> claims = null;
            var user = context?.User;
            if (user != null && user.Claims != null)
            {
                claims = new List<Claim>();
                foreach (var claim in user.Claims)
                {
                    claims.Add(new Claim(claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer));
                }
            }

            return claims;
        }

        /// <summary>
        /// Returns the value from the HTTP Request Header X-Forwarded-Host.
        /// The value is read first from the X-Client-Tag header first.
        /// </summary>
        /// <param name="httpContext">HTTP context.</param>
        /// <returns>Value from the HTTP Request Header X-Forwarded-Host.</returns>
        public static string GetRequestSubDomain(this HttpContext httpContext)
        {
            // Check for the custom header X-Client-Tag to find the request subdomain
            string forwardedHost = httpContext?.Request?.Headers[EncryptionConstants.EncryptionRequestHeader].ToString().ToLowerInvariant();

            // If Custom Header is null find the request subdomain in the X-Forwarded-Host
            if (string.IsNullOrEmpty(forwardedHost))
            {
                forwardedHost = httpContext?.Request?.Headers["X-Forwarded-Host"].ToString().ToLowerInvariant();
            }

            // If all of the above are null return host value
            if (string.IsNullOrEmpty(forwardedHost))
            {
                forwardedHost = httpContext?.Request?.Host.ToString().ToLowerInvariant();
            }

            return forwardedHost;
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase