#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Creates HttpClient instances that will automatically pass user context.
    /// </summary>
    public class HttpContextClientFactory
    {
        /// <summary>
        /// Defines the _includeUserContext.
        /// </summary>
        private readonly bool _includeUserContext;

        /// <summary>
        /// Defines the _includeTrackingId.
        /// </summary>
        private readonly bool _includeTrackingId;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextClientFactory"/> class.
        /// </summary>
        public HttpContextClientFactory() : this(includeUserContext: true, includeTrackingId: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextClientFactory"/> class.
        /// </summary>
        /// <param name="includeUserContext">true to include user context in requests; otherwise, false.</param>
        /// <param name="includeTrackingId">true to include tracking ID in requests; otherwise, false.</param>
        public HttpContextClientFactory(bool includeUserContext, bool includeTrackingId)
        {
            _includeUserContext = includeUserContext;
            _includeTrackingId = includeTrackingId;
        }

        /// <summary>
        /// Creates a new instance of HttpClient.
        /// </summary>
        /// <param name="services">The provider of services.</param>
        /// <returns>A new HttpClient instance.</returns>
        [Obsolete("Use Typed/Named HttpClient with `IHttpClientFactory`.")]
        public static HttpClient Create(IServiceProvider services)
        {
            var factory = new HttpContextClientFactory();
            return factory.CreateClient(services);
        }

        /// <summary>
        /// Creates a new instance of HttpClient.
        /// </summary>
        /// <param name="services">The provider of services.</param>
        /// <returns>A new HttpClient instance.</returns>
        public HttpClient CreateClient(IServiceProvider services)
        {
            var httpContextAccessor = services.GetService<IHttpContextAccessor>();
            var handler = new UserContextDelegatingHandler(httpContextAccessor);

            // Fiddler support
            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            var clientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                Proxy = string.IsNullOrEmpty(httpProxy) ? null : new WebProxy(httpProxy)
            };

            handler.InnerHandler = clientHandler;
            var client = new HttpClient(handler);

            return client;
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA2000 // Dispose objects before losing scope