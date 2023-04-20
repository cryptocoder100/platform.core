namespace Exos.Platform.AspNetCore.IntegrationTests.Extensions
{
    using System;
    using System.Net.Http;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using Polly.Registry;

    /// <summary>
    /// helper extention.
    /// </summary>
    internal static class IServiceCollectionExtensions
    {
        /// <summary>
        /// An IServiceCollection extension method inject HttpClient that:
        /// - Support retry.
        /// - Inject UserContext.
        /// </summary>
        /// <param name="services"> The services to act on.</param>
        /// <param name="handlerLifetimeInMinutes"> The handler lifetime in minutes.</param>
        /// <param name="configuration">The app configuration.</param>
        /// <returns> The updated IServiceCollection.</returns>
        public static IServiceCollection AddExosHttpClient(
            this IServiceCollection services,
            int handlerLifetimeInMinutes,
            IConfiguration configuration)
        {
            var platformDefaults = new PlatformDefaultsOptions();
            configuration.GetSection("PlatformDefaults").Bind(platformDefaults);

            services.AddHttpClient(HttpClientTypeConstants.Context)
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTenancySubdomain()
                .EnrichWithTrackingId()
                .EnrichWithUserContext()
                .SetHandlerLifetime(TimeSpan.FromMinutes(handlerLifetimeInMinutes))
                .AddPolicyHandler((sp, request) =>
                    sp.GetService<IReadOnlyPolicyRegistry<string>>()
                    .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http));

            services.AddHttpClient(HttpClientTypeConstants.Naive)
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .SetHandlerLifetime(TimeSpan.FromMinutes(handlerLifetimeInMinutes))
                .AddPolicyHandler((sp, request) =>
                    sp.GetService<IReadOnlyPolicyRegistry<string>>()
                    .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http));

            return services;
        }
    }
}
