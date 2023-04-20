namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Add HttpContext tracking Id to HttpRequestMessages.
    /// </summary>
    public class TrackingIdDelegatingHandler : DelegatingHandler
    {
        private IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingIdDelegatingHandler"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null.</exception>
        /// <param name="httpContextAccessor"> The HTTP context accessor.</param>
        public TrackingIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Try Add Tracking Id To Request Headers.
        /// </summary>
        /// <param name="request">request.</param>
        /// <param name="httpContextAccessor">httpContextAccessor.</param>
        public static void TryAddTrackingIdToRequestHeaders(HttpRequestMessage request, IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            if (request != null && !request.Headers.TryGetValues("Tracking-Id", out IEnumerable<string> trackingId))
            {
                // Set the Tracking-Id header automatically
                var tid = httpContextAccessor.HttpContext?.GetTrackingId();
                if (!string.IsNullOrEmpty(tid))
                {
                    request.Headers.TryAddWithoutValidation("Tracking-Id", tid);
                }
            }
        }

        /// <inheritdoc/>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TryAddTrackingIdToRequestHeaders(request, _httpContextAccessor);

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
