namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <inheritdoc/>
    public class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private static readonly Regex AuthorizationHeaderRegex = new Regex(@"^(\w+)\s+(.*)", RegexOptions.IgnoreCase);
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationDelegatingHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor.</param>
        public AuthenticationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
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
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TryAddAuthToRequestHeaders(request, _httpContextAccessor);

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
                    // We never want to forward basic auth from svcto svc
                    if (authType.Equals("bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"{authType} {match.Groups[2]}");
                    }
                }
            }
        }
    }
}
