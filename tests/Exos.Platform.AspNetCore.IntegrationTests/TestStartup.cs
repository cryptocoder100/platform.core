#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1506

namespace Exos.Platform.AspNetCore.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.IntegrationTests.Extensions;
    using Exos.Platform.AspNetCore.IntegrationTests.Mock;
    using Exos.Platform.AspNetCore.IntegrationTests.Options;
    using Exos.Platform.AspNetCore.IntegrationTests.Policies;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Registry;

    public class TestStartup
    {
        public TestStartup(IConfiguration config, IWebHostEnvironment env)
        {
            Configuration = config;
            Environment = env;
        }

        public static IConfiguration Configuration { get; set; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // platform dependencies
            services.Configure<UserContextOptions>(Configuration.GetSection("JWT"));
            services.TryAddSingleton<IHttpContextAccessor, MockHttpContextAccessor>();
            services.AddExosPlatformDefaults(Configuration, Environment);

            // add resiliency configurations and policies
            services.Configure<HttpRequestPolicyOptions>(Configuration.GetSection("ResiliencyPolicy:HttpRequestPolicyOptions"));
            services.AddSingleton<IHttpRequestResiliencyPolicy, TestHttpRequestResiliencyPolicy>();

            // this is added from AddPlatformDefaults() and not needed here, but intentionally added.
            services.AddPolicyRegistry();

            // add external dependencies
            services.Configure<ExternalServiceOptions>(Configuration.GetSection("ExternalServiceOptions"));

            var platformDefaults = new PlatformDefaultsOptions();
            Configuration.GetSection("PlatformDefaults").Bind(platformDefaults);

            // register http client
            services.AddExosHttpClient(Configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes"), Configuration);
            services.AddHttpClient("UserSvc", client =>
            {
                var userSvcBaseAddress = Configuration.GetValue<string>("ExternalServiceOptions:ExternalServices:UserSvc:Host");
                client.BaseAddress = new Uri(userSvcBaseAddress);
            })
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTenancySubdomain()
                .EnrichWithTrackingId()
                .EnrichWithUserContext()
                .SetHandlerLifetime(TimeSpan.FromMinutes(Configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes")))
                .AddPolicyHandler((sp, request) =>
                    sp.GetService<IReadOnlyPolicyRegistry<string>>()
                    .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http));

            services.AddHttpClient("NoPolicyAtDI")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTenancySubdomain()
                .EnrichWithTrackingId()
                .EnrichWithUserContext()
                .SetHandlerLifetime(TimeSpan.FromMinutes(Configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes")));

            var noOpPolicy = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
            services.AddHttpClient("RetryOnGetAndPut")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTenancySubdomain()
                .EnrichWithTrackingId()
                .EnrichWithUserContext()
                .SetHandlerLifetime(TimeSpan.FromMinutes(Configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes")))
                .AddPolicyHandler((sp, request) =>
                    sp.GetService<IOptions<HttpRequestPolicyOptions>>().Value.RetryHttpMethod.Contains(request.Method)
                    ? sp.GetService<IReadOnlyPolicyRegistry<string>>().Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http)
                    : noOpPolicy);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            // populate registry here, in Configure we'll have access to service provider
            app.AddResilientPolicies(new Dictionary<string, IResilientPolicy>
            {
                { PolicyRegistryKeys.Http, app.ApplicationServices.GetService<IHttpRequestResiliencyPolicy>() }
            });
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1062 // Validate arguments of public methods