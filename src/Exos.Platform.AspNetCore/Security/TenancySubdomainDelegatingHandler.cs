#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Encryption;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Add HttpContext tracking Id to HttpRequestMessages.
    /// </summary>
    public class TenancySubdomainDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenancySubdomainDelegatingHandler"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null.</exception>
        /// <param name="httpContextAccessor"> The HTTP context accessor.</param>
        public TenancySubdomainDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Add Tenancy Sub-Domain To RequestHeaders.
        /// </summary>
        /// <param name="request">request.</param>
        /// <param name="httpContextAccessor">httpContextAccessor.</param>
        public static void TryAddTenancySubdomainToRequestHeaders(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor != null && request != null)
            {
                // NOTE: We used to pass X-Forwarded-* headers downstream but have since disabled that. We still get the edge domain from the X-Forwarded-* header at
                // top-level services (if configured) which is all we need to determine the original domain endpoint. Passing that (or worse, an intermediate domain)
                // further downstream offers no additional advantage and confuses Application Insights.

                // Pass the value for the Encryption Header
                string encryptionRequestHeaderValue = httpContextAccessor?.HttpContext?.Request?.Headers[EncryptionConstants.EncryptionRequestHeader].ToString().ToLowerInvariant();
                if (!string.IsNullOrEmpty(encryptionRequestHeaderValue))
                {
                    // Check if header is already added
                    request.Headers.TryGetValues(EncryptionConstants.EncryptionRequestHeader, out IEnumerable<string> headerValues);
                    if (headerValues == null || !headerValues.Any())
                    {
                        request.Headers.TryAddWithoutValidation(EncryptionConstants.EncryptionRequestHeader, encryptionRequestHeaderValue);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TryAddTenancySubdomainToRequestHeaders(request, _httpContextAccessor);

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
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase