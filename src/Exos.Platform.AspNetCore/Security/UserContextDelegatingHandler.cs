namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class UserContextDelegatingHandler : DelegatingHandler
    {
        private static readonly Regex AuthorizationHeaderRegex = new Regex(@"^(\w+)\s+(.*)", RegexOptions.IgnoreCase);
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextDelegatingHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The httpContextAccessor<see cref="IHttpContextAccessor"/>.</param>
        public UserContextDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Adds headers needed for authorization of outgoing service requests.
        /// Adds the authorization header from initiating request (if any) and adds the ElevatedRight header for service to service calls.
        /// </summary>
        /// <param name="request">request.</param>
        /// <param name="httpContextAccessor">httpContextAccessor.</param>
        public static void TryAddAuthToRequestHeaders(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            AddAuthorizationHeaderIfNeeded(request, httpContextAccessor);
            AddElevatedRightHeaderIfNeeded(request, httpContextAccessor);
            AddOtherHeaders(request, httpContextAccessor);
        }

        /// <summary>
        /// Adds work order tenant headers to outgoing service requests.
        /// </summary>
        /// <param name="request">The outgoing request.</param>
        /// <param name="httpContextAccessor">An <see cref="IHttpContextAccessor" /> instance for the current operation.</param>
        public static void TryAddWorkOrderTenantHeader(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            if (request == null || request.Headers.Contains("Exos-Work-Order-Tenant"))
            {
                return;
            }

            var context = httpContextAccessor?.HttpContext;
            var logger = context?.RequestServices?.GetService<ILogger<UserContextDelegatingHandler>>() ?? NullLogger<UserContextDelegatingHandler>.Instance;
            var options = context?.RequestServices?.GetService<IOptions<UserContextOptions>>()?.Value;

            // Do we meet preconditions for sending tenant information in a header?
            if (options?.IncludeOutboundWorkOrderTenantHeader == null || !options.IncludeOutboundWorkOrderTenantHeader)
            {
                logger.LogDebug("The 'Exos-Work-Order-Tenant' outbound header will not be included because the feature is disabled.");
                return;
            }

            var key = UserContextHelper.GetWorkOrderTenantHeaderSigningSecretBytesSafe(options); // Null-safe
            if (key == null || key.Length == 0)
            {
                logger.LogDebug("The 'Exos-Work-Order-Tenant' outbound header will not be included because a signing secret is not configured.");
                return;
            }

            var user = context?.User?.Identity as ClaimsIdentity;
            if (!UserContextHelper.TryGetWorkOrderTenantFromClaims(user, out var tenant))
            {
                logger.LogInformation("The 'Exos-Work-Order-Tenant' outbound header will not be included because no tenant data is in the current operation.");
                return;
            }

            if (!request.Headers.TryGetValues("workorderid", out var outboundWorkOrderId) || !string.Equals(tenant.WorkOrderId, outboundWorkOrderId.FirstOrDefault(), StringComparison.Ordinal))
            {
                logger.LogWarning(
                    $"The 'Exos-Work-Order-Tenant' outbound header will not be included because the tenant work order ID does not match the work order ID in the outbound 'workorderid' header. " +
                    $"This may indicate that the work order is reassigned.");

                return;
            }

            // Add as a signed header
            var value = UserContextHelper.SerializeAndSignWorkOrderTenant(tenant, key);
            request.Headers.TryAddWithoutValidation("Exos-Work-Order-Tenant", value);
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TryAddAuthToRequestHeaders(request, _httpContextAccessor);
            TryAddWorkOrderTenantHeader(request, _httpContextAccessor);

            try
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                // Wrapping exceptions is usually discouraged, however:
                // a) We still include the base exception
                // b) We want to include some additional information about the call
                throw new HttpRequestException($"An error occurred while sending the request. Uri: '{request?.RequestUri?.AbsoluteUri}', Method: {request?.Method?.Method}.", ex);
            }
        }

        private static void AddOtherHeaders(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            if (request == null)
            {
                return;
            }

            var inboundRequest = httpContextAccessor?.HttpContext?.Request;
            if (inboundRequest == null)
            {
                return;
            }

            CopyInboundHeaderToRequest(inboundRequest, request, "workorderid");
            CopyInboundHeaderToRequest(inboundRequest, request, "servicertenantidentifier");
        }

        private static void CopyInboundHeaderToRequest(HttpRequest inboundRequest, HttpRequestMessage outboundRequest, string headerName)
        {
            if (inboundRequest.Headers.TryGetValue(headerName, out var workOrderValues))
            {
                if (outboundRequest.Headers.Contains(headerName) == false)
                {
                    outboundRequest.Headers.TryAddWithoutValidation(headerName, workOrderValues.ToArray());
                }
            }
        }

        private static void AddAuthorizationHeaderIfNeeded(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            if (request == null || request.Headers.Contains("Authorization"))
            {
                return;
            }

            var inboundRequest = httpContextAccessor?.HttpContext?.Request;
            if (inboundRequest == null)
            {
                return;
            }

            // Copy the inbound Authorization header to the outbound request
            if (inboundRequest.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                var match = AuthorizationHeaderRegex.Match(authorizationHeader);
                if (match.Success)
                {
                    var authType = match.Groups[1].Value;
                    var authValue = match.Groups[2].Value;

                    // For tranisitoning from Basic auth to JWT for downstream calls
                    if (authType.Equals("basic", StringComparison.OrdinalIgnoreCase))
                    {
                        if (httpContextAccessor.HttpContext.User.Claims.Any(c =>
                            c.Type == ClaimConstants.ApiKeyAccessToken))
                        {
                            authType = "Bearer";
                            authValue = httpContextAccessor.HttpContext.User.Claims.Single(c =>
                                c.Type == ClaimConstants.ApiKeyAccessToken).Value;
                        }
                    }

                    request.Headers.TryAddWithoutValidation("Authorization", $"{authType} {authValue}");
                }
            }
        }

        private static void AddElevatedRightHeaderIfNeeded(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            if (request == null || request.Headers.Contains("ElevatedRight"))
            {
                return;
            }

            var inboundRequest = httpContextAccessor?.HttpContext?.Request;

            var signatureClaim = inboundRequest?.HttpContext?.User?.Claims?.SingleOrDefault(c => c.Type == ExosClaimTypes.ClaimsSignature);

            if (signatureClaim == null)
            {
                return;
            }

            request.Headers.TryAddWithoutValidation("ElevatedRight", signatureClaim.Value);
        }
    }
}
