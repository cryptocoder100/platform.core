using System;
using System.Linq;
using Exos.Platform.AspNetCore.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Exos.Platform.AspNetCore.Extensions
{
    /// <summary>
    /// IHttpClientBuilder extension methods.
    /// </summary>
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// An IHttpClientBuilder extension method that enrich HttpRequestMessages with tracking id.
        /// </summary>
        /// <param name="builder"> The builder to act on.</param>
        /// <returns>The builder.</returns>
        public static IHttpClientBuilder EnrichWithTrackingId(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddTransient<TrackingIdDelegatingHandler>();
            builder.AddHttpMessageHandler<TrackingIdDelegatingHandler>();
            return builder;
        }

        /// <summary>
        /// An IHttpClientBuilder extension method that enrich with UserContext.
        /// </summary>
        /// <param name="builder"> The builder to act on.</param>
        /// <returns>The builder.</returns>
        public static IHttpClientBuilder EnrichWithUserContext(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddTransient<UserContextDelegatingHandler>();
            builder.AddHttpMessageHandler<UserContextDelegatingHandler>();
            return builder;
        }

        /// <summary>
        /// An IHttpClientBuilder extension method that adds
        /// headers needed for authorization of outgoing service requests.
        /// </summary>
        /// <param name="builder"> The builder to act on.</param>
        /// <returns>The builder.</returns>
        public static IHttpClientBuilder EnrichWithAuthenticationOnly(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddTransient<AuthenticationDelegatingHandler>();
            builder.AddHttpMessageHandler<AuthenticationDelegatingHandler>();
            return builder;
        }

        /// <summary>
        /// An IHttpClientBuilder extension method that enrich HttpRequestMessages with TenancySubdomain.
        /// </summary>
        /// <param name="builder"> The builder to act on.</param>
        /// <returns>The builder.</returns>
        public static IHttpClientBuilder EnrichWithTenancySubdomain(this IHttpClientBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddTransient<TenancySubdomainDelegatingHandler>();
            builder.AddHttpMessageHandler<TenancySubdomainDelegatingHandler>();
            return builder;
        }
    }
}
