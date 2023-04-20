#pragma warning disable SA1600
#pragma warning disable CA1506
namespace Exos.MinimalPlatformApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Exos.MinimalPlatformApi.Repositories;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.HealthCheck;
    using Exos.Platform.AspNetCore.KeyVault;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Registry;

    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void Configure(IApplicationBuilder app)
        {
            // Platform Defaults
            app.AddExosPlatformDefaults();

            // CORS
            app.AddExosCorsPolicy(Configuration);

            // Serve version information at /version
            // app.UseExosVersionEndpoint();

            // Allow long GET URLs to be sent as POSTs
            app.UseHttpMethodOverride();

            // Serve version information at /version
            app.UseExosVersionEndpoint();

            // Throw 404 for anything not handled
            app.RunNotFoundException();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Applies the standard configuration for JSON serialization and configures all
            // controller actions to require authentication. Use the AllowAnonymous attribute on
            // controllers or actions you want anonymous access to. For more control over platform
            // defaults, see the code in the ServiceCollectionExtensions.AddPlatformDefaults method.
            services.AddExosPlatformDefaults(Configuration, Environment);

            // Allow NGINX/GatewaySvc to pass us X-Forwarded-For, X-Forwarded-Proto, and X-Forwarded-Host headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
            });

            // Platform services
            services.AddScoped<IUserContextAccessor, UserContextAccessor>();
            services.AddTransient<IUserAccessor, UserAccessor>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Distributed cache
            services.AddExosRedisConnectionPool(options =>
            {
                options.Configuration = Configuration.GetConnectionString("DistributedRedisCache");
                options.PoolSize = Configuration.GetValue("ExosDistributedCache:PoolSize", 3);
            });
            services.AddExosRedisDistributedCache(options =>
            {
                options.MaxRetries = Configuration.GetValue("ExosDistributedCache:MaxRetries", 6);
            });

            // Register the configuration options for handling user context tokens
            services.Configure<UserContextOptions>(Configuration.GetSection("UserContext"));

            services.Configure<ApiKeyAuthenticationOptions>(Configuration.GetSection("ApiKey"));

            services.AddAuthentication().AddApiKey(options =>
            {
                options.RedisConfiguration = Configuration.GetValue<string>("Redis:ReadWriteConnectionString");
                options.UserSvc = Configuration.GetValue<string>("ApiKey:UserSvc");
            });

            // Register HttpClient as DI injectable using our custom factory that automatically
            // handles user context and tracking id propagation.
            // HTTP Client Configuration
            services.Configure<HttpRequestPolicyOptions>(Configuration.GetSection("ResiliencyPolicy:HttpRequestPolicyOptions"));
            services.AddSingleton<IHttpRequestResiliencyPolicy, HttpRequestResiliencyPolicy>();

            services.AddSingleton<IApiKeyMappingConfiguration>(_ => (IApiKeyMappingConfiguration)Configuration.GetSection("ApiKeyMapping"));

            var platformDefaults = new PlatformDefaultsOptions();
            Configuration.GetSection("PlatformDefaults").Bind(platformDefaults);

            // HTTP Client with Authentication Header and Tracking Id
            services.AddHttpClient("Client_Authentication_Header_and_Tracking_Id")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTrackingId()
                .EnrichWithUserContext()
                .SetHandlerLifetime(TimeSpan.FromMinutes(
                    Configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes")))
                .AddPolicyHandler((sp, request) =>
                {
                    var options = sp.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
                    return !options.IsDisabled && options.RetryHttpMethod.Contains(request.Method)
                        ? sp.GetService<IReadOnlyPolicyRegistry<string>>()
                            .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http)
                        : Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                });

            // HTTP Client with No Headers
            services.AddHttpClient("Client_No_Header")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults))
                .EnrichWithTrackingId()
                .SetHandlerLifetime(TimeSpan.FromMinutes(
                    Configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes")))
                .AddPolicyHandler((sp, request) =>
                {
                    var options = sp.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
                    return !options.IsDisabled && options.RetryHttpMethod.Contains(request.Method)
                        ? sp.GetService<IReadOnlyPolicyRegistry<string>>()
                            .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http)
                        : Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
                });

            services.Configure<AzureKeyVaultSettings>(Configuration.GetSection("AzureKeyVault"));
            services.AddSingleton<AzureKeyVaultKeyClient>();
            services.AddSingleton<IAppTokenProvider, B2CAppTokenProvider>();

            // Generate a Swagger documentation file
            services.AddSwagger();

            services.AddHealthChecks()
                .AddCheck("FatalException", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
                .AddCheck<DistributedCacheHealthCheck>("DistributedCache", tags: new[] { "ready" });

            // Mask sensitive data in telemetry logs
            List<string> maskTelemetryValues = Configuration.GetSection("MaskTelemetryValues")
                .GetChildren().AsEnumerable()
                .Select(maskValue => maskValue.Value).ToList();

            services.MaskTelemetryValues(maskTelemetryValues);

            // Build information
            services.AddExosBuildInformation();

            // Add the API's specific services
            services.AddSingleton<IValuesRepository, ValuesMemoryRepository>();

            // Example named dependencies
            services.AddNamedSingleton<IValuesRepository>(builder =>
            {
                builder.Add<ValuesMemoryRepository>("a");
                builder.Add<ValuesMemoryRepository>("b");
                builder.Add("c", sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<ValuesMemoryRepository>>();
                    return new ValuesMemoryRepository(logger);
                });
            });
        }
    }
}
#pragma warning restore SA1600
#pragma warning restore CA1506