#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable CA1506 // Avoid excessive class coupling
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1600 // Elements should be documented
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.AppConfiguration;
using Exos.Platform.AspNetCore.Authentication;
using Exos.Platform.AspNetCore.HealthCheck;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Middleware;
using Exos.Platform.AspNetCore.Models;
using Exos.Platform.AspNetCore.Resiliency.Policies;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.AspNetCore.Telemetry;
using Exos.Platform.Telemetry;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IO;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Polly.Registry;

namespace Exos.Platform.AspNetCore.Extensions
{
    /// <summary>
    /// Extension methods for the IServiceCollection interface.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds to the services collection platform defaults for JsonSerializerSettings, MvcJsonOptions, and MvcOptions.
        /// </summary>
        /// <param name="services">The IServiceCollection to configure.</param>
        /// <param name="configuration">An <see cref="IConfiguration" /> instance.</param>
        /// <param name="environment">An <see cref="IWebHostEnvironment" /> instance.</param>
        /// <returns>The updated IServiceCollection.</returns>
        public static IServiceCollection AddExosPlatformDefaults(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _ = environment ?? throw new ArgumentNullException(nameof(environment));

            services.AddFeatureManagement();
            if (ConfigurationHelper.IsAppConfigurationEnabled(configuration))
            {
                // Get values from App Configuration and Key Vault
                services.PerformConfigurationTokenResolution(configuration);
                services.AddAzureAppConfiguration();
            }

            services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
            {
                EnableAdaptiveSampling = false,
                InstrumentationKey = configuration.GetValue<string>("ApplicationInsights:InstrumentationKey"),
            });

            services.TryAddTelemetryInitializer<GlobalEnricherTelemetryInitializer>();
            services.TryAddTelemetryInitializer<RequestEnricherTelemetryInitializer>();
            services.TryAddTelemetryInitializer<ExceptionEnricherTelemetryInitializer>();
            services.AddExosTrackingIdTelemetryInitializer();
            services.AddExosIgnoreTelemetryProcessor();
            services.AddExosUserInfoTelemetryInitializer();
            services.AddApplicationInsightsTelemetryProcessor<InProcTelemetryProcessor>();
            services.AddApplicationInsightsTelemetryProcessor<DependencyFilterTelemetryProcessor>();

            // Mask sensitive data in telemetry logs this are common on all the services
            services.MaskTelemetryValues(new List<string>()
            {
                "password",
                "ssn",
                "oauth_token",
                "headercsrftoken",
                "UserClaimsCacheKey",
                "UserClaimsWorkOrdersCacheKey",
                "userContextToken",
                "username",
                "auth"
            });

            // Track the SQL command text in SQL dependencies.
            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) => { module.EnableSqlCommandTextInstrumentation = true; });

            // Authentication
            var platformDefaults = new PlatformDefaultsOptions();
            configuration.GetSection("PlatformDefaults").Bind(platformDefaults);
            services.AddSingleton<IOptions<PlatformDefaultsOptions>>(Options.Create(platformDefaults));
            services.AddSingleton<IAuthorizationHandler, ApiResourceAuthorizationHandler>();
            if (configuration.GetValue("OAuth:Enabled", false))
            {
                // Standard stuff for JWT authentication
                var schemes = configuration.GetSection("OAuth:AuthSchemes").Get<List<string>>();
                foreach (var scheme in schemes)
                {
                    _ = services.AddAuthentication().AddJwtBearer(scheme, options =>
                    {
                        options.Authority = configuration.GetValue<string>("OAuth:" + scheme + ":Authority");
                        var audience = configuration.GetSection("OAuth:" + scheme + ":Audience").Get<List<string>>();
                        var nameClaimType = configuration.GetValue<string>("OAuth:" + scheme + ":NameClaimType");
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuers = configuration.GetSection("OAuth:" + scheme + ":ValidIssuers").Get<List<string>>(),
                            ValidateAudience = audience != null,
                            ValidAudiences = audience,
                            NameClaimType = nameClaimType ?? ClaimTypes.Email
                        };
                        options.SaveToken = true;

                        // TODO This requires more research to determine the effect it could have on signing key rotation.
                        // For now, it seems to be the only way to keep the handler from re-querying for signing keys.
                        options.RefreshOnIssuerKeyNotFound = false;

                        options.Events = new JwtBearerEvents()
                        {
                            OnAuthenticationFailed = context =>
                            {
                                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                                {
                                    string authorizationHeader = context.HttpContext.Request.Headers["Authorization"];
                                    string jwtToken = authorizationHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
                                    var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                                    var jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(jwtToken);

                                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                                    var logger = loggerFactory.CreateLogger("JWTTokenExpired");
                                    logger.LogError(
                                        context.Exception,
                                        "JWT Token Expired - Scheme: {Scheme} - " +
                                        "Token Expiration: {TokenExpirationTime} - " +
                                        "Token Valid From: {TokenValidFrom} " +
                                        "Current Time:{TokenCurrentTime}",
                                        context.Scheme.Name,
                                        ((SecurityTokenExpiredException)context.Exception).Expires,
                                        jwtSecurityToken.ValidFrom,
                                        DateTimeOffset.UtcNow);
                                }

                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }

            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            // CORS
            var corsOrigins = configuration?["ServiceCorsOrigins"]?.Split(',');
            services.AddCors(opt =>
            {
                opt.AddDefaultPolicy(builder =>
                {
                    if (corsOrigins?.Any() == true)
                    {
                        builder
                            .WithOrigins(corsOrigins.ToArray())
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .SetIsOriginAllowedToAllowWildcardSubdomains();
                    }
                    else
                    {
                        builder.SetIsOriginAllowed(host => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
            });

            // Secure all controllers and actions by default.
            // To allow anonymous access, use the AllowAnonymous attribute.
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new UserContextRequirement())
                .Build();

            services.AddScoped(sp => new AuthorizeFilter(policy));
            var mvcBuilder = services.AddControllers(options => options.Filters.Add(new AuthorizeFilter(policy)));

            if (platformDefaults.NewtonsoftJsonCompatability)
            {
                mvcBuilder.AddNewtonsoftJson(options =>
                {
                    options.AddPlatformDefaults(environment);
                });

                // Added to force Swagger to use Newtonsoft.Json instead of System.Text.Json
                // Also the Swashbuckle.AspNetCore.Newtonsoft library was included
                services.AddSwaggerGenNewtonsoftSupport();
            }
            else
            {
                mvcBuilder.AddJsonOptions(options =>
                {
                    options.AddPlatformDefaults(environment);
                });
            }

            // NewtonsoftJsonCompatability flag only applies to ASP.NET pipeline.
            // For all other uses (e.g. JsonConvert) we preserve compatibility.
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                return settings.AddPlatformDefaults(environment);
            };

            // Registers an empty Polly.Registry.PolicyRegistry in the service collection
            services.AddPolicyRegistry();

            services.AddExosNativeHttpClient(configuration);

            // Allow NGINX/GatewaySvc to pass us X-Forwarded-For, X-Forwarded-Proto, and X-Forwarded-Host headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
            });

            // Add ThreadPool
            services.AddThreadPoolService(configuration);

            // Recyclable Memory Stream
            services.AddSingleton<RecyclableMemoryStreamManager, RecyclableMemoryStreamManager>();

            services.AddSingleton<IConfiguration>(configuration);

            // Usually already added by the framework, but we call explicitly to be sure
            // (safe to call multiple times if also registered by user services)
            services.AddMemoryCache();

            // Default middleware
            services.AddSingleton<RequestTelemetryMiddleware>();
            services.AddSingleton<IStartupFilter, RequestTelemetryMiddlewareStartupFilter>();
            services.AddSingleton<IStartupFilter, ExosAzureAppConfigurationStartupFilter>();

            return services;
        }

        /// <summary>
        /// Performs configuration token resolution.
        /// </summary>
        /// <param name="services">The IServiceCollection to configure.</param>
        /// <param name="configuration">The IConfiguration being resolved.</param>
        /// <returns>The updated IServiceCollection.</returns>
        public static IServiceCollection PerformConfigurationTokenResolution(this IServiceCollection services, IConfiguration configuration)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Custom ExosToken Resolution
            ExosAzureConfigurationResolutionProcessor.ProcessTokenResolution(configuration);

            return services;
        }

        /// <summary>
        /// Configure the Thread Pool Service.
        /// </summary>
        /// <param name="services">The IServiceCollection to configure.</param>
        /// <param name="configuration">An <see cref="IConfiguration" /> instance.</param>
        /// <returns>The updated IServiceCollection.</returns>
        public static IServiceCollection AddThreadPoolService(this IServiceCollection services, IConfiguration configuration)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            if (configuration.GetSection("ThreadPoolOptions").Exists())
            {
                services.Configure<ThreadPoolOptions>(configuration.GetSection("ThreadPoolOptions"));
            }
            else
            {
                services.Configure<ThreadPoolOptions>(options =>
                {
                    options.MinWorkerThreads = "50";
                    options.MinCompletionPortThreads = "50";
                });
            }

            services.AddSingleton<IThreadPoolService, ThreadPoolService>();
            // Instantiate the Thread Pool Service
            services.AddHealthChecks().AddCheck<ThreadPoolHealthCheck>("ThreadPool Health Check", tags: new[] { "ready" });
            return services;
        }

        [Obsolete("Calling AddExosPlatformDefaults now includes standard paths implicitly.")]
        public static IServiceCollection SuppressTelemetryFromPaths(this IServiceCollection services, params PathString[] paths)
        {
            return services;
        }

        [Obsolete("Calling AddExosPlatformDefaults now includes standard paths implicitly.")]
        public static IServiceCollection SuppressTelemetryFromPaths(this IServiceCollection services, SuppressTelemetryRequestModel suppressTelemetryRequestModel, params PathString[] paths)
        {
            return services;
        }

        /// <summary>
        /// Add Polly Resiliency Registry.
        /// </summary>
        /// <param name="services">The IServiceCollection to configure.<see cref="IServiceCollection"/>.</param>
        /// <param name="sp"><see cref="IServiceProvider"/>.</param>
        /// <returns><see cref="IServiceProvider"/> with Polly policies.</returns>
        public static IServiceCollection AddBlobStorageResiliencyPolicy(this IServiceCollection services, IServiceProvider sp)
        {
            var servicerProvider = sp;
            var configuration = servicerProvider.GetService<IConfiguration>();
            services.Configure<BlobStorageResiliencyPolicyOptions>(configuration.GetSection("ResiliencyPolicy:BlobStorageResiliencyPolicyOptions"));

            services.AddSingleton<IBlobStorageResiliencyPolicy, BlobStorageResiliencyPolicy>();
            sp = services.BuildServiceProvider();

            var blobPolicy = sp.GetRequiredService<IBlobStorageResiliencyPolicy>();

            var policyRegistry = new PolicyRegistry
            {
                { PolicyRegistryKeys.BlobStorageResiliencyPolicy, blobPolicy.ResilientPolicy },
            };

            services.AddPolicyRegistry(policyRegistry);
            return services;
        }

        /// <summary>
        /// Adds the typical Swagger configuration.  It will include xml comments if commentFileName is included.
        /// </summary>
        /// <param name="services">The current IServiceCollection.</param>
        /// <param name="commentFileName">The name of the xml file containing xml comments. This is expected to be in PlatformServices.Default.Application.ApplicationBasePath directory. .</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddSwagger(this IServiceCollection services, string commentFileName = null)
        {
            services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = $"{AssemblyHelper.EntryAssemblyName} API",
                    Version = AssemblyHelper.EntryAssemblyVersion.ToString()
                });
                setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                setup.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authorization."
                });

                if (string.IsNullOrWhiteSpace(commentFileName))
                {
                    commentFileName = $"{AssemblyHelper.EntryAssemblyName}.xml";
                }

                var basePath = System.AppContext.BaseDirectory;
                var xmlPath = Path.Combine(basePath, commentFileName);
                if (File.Exists(xmlPath))
                {
                    setup.IncludeXmlComments(xmlPath);
                }

                setup.CustomSchemaIds(i => i.FullName);

                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });

                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "basic"
                                }
                            },
                          Array.Empty<string>()
                    }
                });
            });
            return services;
        }

        /// <summary>
        /// Add common Exos Platform Enhancements.
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/>.</param>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <param name="environment">The environment<see cref="IWebHostEnvironment"/>.</param>
        public static void AddExosPlatformEnhancements(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            // CORS
            services.AddCors();

            // Allow NGINX/GatewaySvc to pass us X-Forwarded-For, X-Forwarded-Proto, and X-Forwarded-Host headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
            });

            // Platform services
            services.AddScoped<IUserContextAccessor, UserContextAccessor>();
            services.AddTransient<IUserAccessor, UserAccessor>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Generate a Swagger documentation file
            services.AddSwagger();

            // Health checks
            services.AddHealthChecks()
                .AddCheck("FatalException", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
        }

        /// <summary>
        /// Mask sensitive data in telemetry logs.
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/>.</param>
        /// <param name="maskTelemetryValues">List of values to mask.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection MaskTelemetryValues(this IServiceCollection services, List<string> maskTelemetryValues)
        {
            if (maskTelemetryValues != null && maskTelemetryValues.Any())
            {
                services.Configure<MaskTelemetryProcessorOptions>(maskTelemetryProcessorOptions =>
                {
                    maskTelemetryProcessorOptions.MaskTelemetryValues = maskTelemetryValues;
                });
                services.AddApplicationInsightsTelemetryProcessor<MaskTelemetryProcessor>();
            }

            return services;
        }
    }
}
#pragma warning restore CA1506 // Avoid excessive class coupling
#pragma warning restore SA1118 // Parameter should not span multiple lines