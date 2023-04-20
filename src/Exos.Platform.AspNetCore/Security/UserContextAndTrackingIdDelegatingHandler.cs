namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class UserContextAndTrackingIdDelegatingHandler : DelegatingHandler
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserContextOptions _userContextOptions;
        private readonly bool _includeUserContext;
        private readonly bool _includeTrackingId;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextAndTrackingIdDelegatingHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor.</param>
        /// <param name="options">UserContextOptions.</param>
        /// <param name="distributedCache"><see cref="IDistributedCache"/>.</param>
        /// <param name="includeUserContext">indicate whether to include UserContext check.</param>
        /// <param name="includeTrackingId">indicate whether to include TrackingId check.</param>
#pragma warning disable CA2000 // Disposal has been handled in DelegatingHandler class
        public UserContextAndTrackingIdDelegatingHandler(
            IHttpContextAccessor httpContextAccessor,
            IOptions<UserContextOptions> options,
            IDistributedCache distributedCache,
            bool includeUserContext,
            bool includeTrackingId) : base(CreateInnerHandler())
#pragma warning restore CA2000 // Disposal has been handled in DelegatingHandler class
        {
            _distributedCache = distributedCache;
            if (_includeUserContext = includeUserContext)
            {
                _userContextOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
                _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            if (_includeTrackingId = includeTrackingId)
            {
                _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            }
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_includeUserContext)
            {
                UserContextDelegatingHandler.TryAddAuthToRequestHeaders(request, _httpContextAccessor);
                UserContextDelegatingHandler.TryAddWorkOrderTenantHeader(request, _httpContextAccessor);
            }

            if (_includeTrackingId)
            {
                TrackingIdDelegatingHandler.TryAddTrackingIdToRequestHeaders(request, _httpContextAccessor);
            }

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

        /// <summary>
        /// Create HTTP Client Handler.
        /// </summary>
        /// <returns>Configured instance of HttpClientHandler.</returns>
        private static HttpClientHandler CreateInnerHandler()
        {
            // Fiddler support
            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                Proxy = string.IsNullOrEmpty(httpProxy) ? null : new WebProxy(httpProxy)
            };

            return handler;
        }
    }
}
