#pragma warning disable CA2201 // Do not raise reserved exception types
namespace Exos.Platform.Persistence.Policies
{
    using System;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Polly.Registry;

    /// <summary>
    /// Defines the <see cref="ExosRetrySqlPolicyConfigurationExtension" />.
    /// </summary>
    public static class ExosRetrySqlPolicyConfigurationExtension
    {
        /// <summary>
        /// Configure Exos Retry Sql Policy.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddExosRetrySqlPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration != null)
            {
                if (configuration.GetSection("ResiliencyPolicy:SqlRetryPolicyOptions").Exists())
                {
                    services.Configure<SqlRetryPolicyOptions>(configuration.GetSection("ResiliencyPolicy:SqlRetryPolicyOptions"));
                    if (configuration.GetSection("ResiliencyPolicy:SqlRetryPolicyOptions:CommandTimeout").Exists())
                    {
                        DapperExtensions.SetCommandTimeout(configuration.GetValue<int>("ResiliencyPolicy:SqlRetryPolicyOptions:CommandTimeout"));
                    }
                    else
                    {
                        DapperExtensions.SetCommandTimeout(30);
                    }
                }
                else
                {
                    services.Configure<SqlRetryPolicyOptions>(options =>
                    {
                        options.MaxRetries = 5;
                        options.MaxRetryDelay = 15;
                        options.CommandTimeout = 30;
                    });
                    DapperExtensions.SetCommandTimeout(30);
                }
            }
            else
            {
                services.Configure<SqlRetryPolicyOptions>(options =>
                {
                    options.MaxRetries = 5;
                    options.MaxRetryDelay = 15;
                    options.CommandTimeout = 30;
                });
                DapperExtensions.SetCommandTimeout(30);
            }

            services.AddSingleton<IExosRetrySqlPolicy, ExosRetrySqlPolicy>();

            return services;
        }

        /// <summary>
        /// Configure Exos Retry Sql Policy.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceProvider AddExosRetrySqlPolicy(this IServiceProvider serviceProvider)
        {
            var policyRegistry = serviceProvider.GetService<IPolicyRegistry<string>>();
            if (policyRegistry == null)
            {
                throw new NullReferenceException("AddExosPlatformDefaults() is mandatory in ConfigureServices method.");
            }

            if (!policyRegistry.ContainsKey(PolicyRegistryKeys.SqlResiliencyPolicy))
            {
                var exosRetrySqlPolicy = serviceProvider.GetService<IExosRetrySqlPolicy>();
                if (exosRetrySqlPolicy != null)
                {
                    policyRegistry.Add(PolicyRegistryKeys.SqlResiliencyPolicy, exosRetrySqlPolicy.ResilientPolicy);
                }
            }

            // Configure Dapper Extensions
            DapperExtensions.SetExosRetrySqlPolicy(policyRegistry);
            return serviceProvider;
        }

        /// <summary>
        /// Configure Exos Retry Sql Policy.
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder AddExosRetrySqlPolicy(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            AddExosRetrySqlPolicy(app.ApplicationServices);
            return app;
        }
    }
}
#pragma warning restore CA2201 // Do not raise reserved exception types