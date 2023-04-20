namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Globalization;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Enables support for taking the UserId and Email from http headers.
    /// </summary>
    public class ExosImpersonationMiddleware
    {
        private const string AdditionalUserContextHeader = "Exos-AdditionalUserContext";
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosImpersonationMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next RequestDelegate.</param>
        public ExosImpersonationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// Catch any failure and return a formatted error response in JSON format.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var headers = context.Request.Headers;

            var additionalContext = GetHeaderValue(headers, AdditionalUserContextHeader);
            if (string.IsNullOrEmpty(additionalContext) == false)
            {
                ProcessAdditionalContext(additionalContext, context.User);
            }

            return _next(context);
        }

        internal static void ProcessAdditionalContext(string additionalContext, ClaimsPrincipal principal)
        {
            var bits = additionalContext?.Split('~');
            if (bits == null || bits.Length != 2 || bits[0].Length == 0 || bits[1].Length == 0)
            {
                throw new AuthenticationException("Invalid additional context details");
            }

            if (Security.ApiKeySecurity.Version1.ValidatePassword(bits[0], bits[1]) == false)
            {
                throw new AuthenticationException("Invalid additional context details");
            }

            var credentialBits = bits[0].Split('|');
            if (credentialBits == null || credentialBits.Length != 3 || credentialBits[0].Length == 0 || credentialBits[1].Length == 0 || credentialBits[2].Length == 0)
            {
                throw new AuthenticationException("Invalid additional context details");
            }

            var tokenTicks = long.Parse(credentialBits[2], CultureInfo.CurrentCulture);
            var tokenDate = DateTimeOffset.FromUnixTimeSeconds(tokenTicks).UtcDateTime;

            if (tokenDate < DateTime.UtcNow)
            {
                throw new AuthenticationException("Invalid additional context details");
            }

            AddUpdateClaim(principal, ClaimTypes.NameIdentifier, credentialBits[0]);
            AddUpdateClaim(principal, ClaimTypes.Email, credentialBits[1]);
        }

        private static string GetHeaderValue(IHeaderDictionary headers, string headerName)
        {
            var headerData = headers[headerName];
            string val = null;
            if (!string.IsNullOrEmpty(headerData))
            {
                val = headerData.ToString();
            }

            return val;
        }

        private static void AddUpdateClaim(IPrincipal currentPrincipal, string key, string value)
        {
            if (!(currentPrincipal.Identity is ClaimsIdentity identity))
            {
                return;
            }

            // check for existing claim and remove it
            var existingClaim = identity.FindFirst(key);
            if (existingClaim != null)
            {
                identity.RemoveClaim(existingClaim);
            }

            // add new claim
            identity.AddClaim(new Claim(key, value));
        }
    }
}