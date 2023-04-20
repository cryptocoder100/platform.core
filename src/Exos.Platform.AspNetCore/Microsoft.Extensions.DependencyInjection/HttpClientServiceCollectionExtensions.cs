namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Net.Http;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Registry;

    /// <summary>
    /// Extension methods for configuring <see cref="HttpClient" /> in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class HttpClientServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the <c>"ExosNative"</c> and <c>"AuthOnlyForGetClaims"</c> <see cref="IHttpClientFactory" /> instances to the collection.
        /// </summary>
        /// <param name="services">An <see cref="IServiceCollection" /> instance..</param>
        /// <param name="configuration">An <see cref="IConfiguration" /> instance.</param>
        /// <remarks>The registered factories are used to support EXOS authentication in the <see cref="UserContextMiddleware" /> and should not be used for other purposes.</remarks>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddExosNativeHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var handlerLifetimeInMinutes =
                configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes", 30);

            var platformDefaults = new PlatformDefaultsOptions();
            configuration.GetSection("PlatformDefaults").Bind(platformDefaults);

            services.AddHttpClient("ExosNative")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTrackingId()
                .SetHandlerLifetime(TimeSpan.FromMinutes(handlerLifetimeInMinutes))
                .AddPolicyHandler((sp, request) =>
                {
                    var options = sp.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
                    return !options.IsDisabled && options.RetryHttpMethod.Contains(request.Method)
                        ? sp.GetService<IReadOnlyPolicyRegistry<string>>()
                            .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http)
                        : Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                });

            services.AddHttpClient("AuthOnlyForGetClaims")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTrackingId()
                .EnrichWithAuthenticationOnly()
                .SetHandlerLifetime(TimeSpan.FromMinutes(handlerLifetimeInMinutes))
                .AddPolicyHandler((sp, request) =>
                {
                    var options = sp.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
                    return !options.IsDisabled && options.RetryHttpMethod.Contains(request.Method)
                        ? sp.GetService<IReadOnlyPolicyRegistry<string>>()
                            .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http)
                        : Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                });

            return services;
        }
    }
}
