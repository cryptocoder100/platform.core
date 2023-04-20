using System;
using Exos.Platform.AspNetCore.Authentication;
using Exos.Platform.AspNetCore.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Exos.Platform.AspNetCore.Extensions
{
    /// <summary>
    /// IWebHostBuilder Extensions.
    /// </summary>
    public static class IWebHostBuilderExtensions
    {
        /// <summary>
        /// Add external json files and azure key vault configuration sources.
        /// </summary>
        /// <param name="builder">IWebHostBuilder to configure.</param>
        /// <param name="externalJsonFilesConfigurationSectionPath">Alternate path to a configuration section.</param>
        /// <param name="azureKeyVaultConfigurationSectionPath">Alternate path to the Azure Key Vault configuration section.</param>
        /// <returns>Configured IWebHostBuilder.</returns>
        public static IWebHostBuilder UsePlatformConfigurationDefaults(this IWebHostBuilder builder, string externalJsonFilesConfigurationSectionPath = null, string azureKeyVaultConfigurationSectionPath = null)
        {
            builder.UsePlatformExternalJsonFilesConfiguration(externalJsonFilesConfigurationSectionPath);
            builder.UsePlatformAzureKeyVaultConfiguration(azureKeyVaultConfigurationSectionPath);

            builder.UseExosConfiguration();

            return builder;
        }

        /// <summary>
        /// Add external json files to the configuration sources.  The files to add are be found in the
        /// "ExternalJsonFile" configuration section unless otherwise specified.
        /// </summary>
        /// <param name="builder">IWebHostBuilder to configure.</param>
        /// <param name="path">Alternate path to a configuration section.</param>
        /// <returns>Configured IWebHostBuilder.</returns>
        public static IWebHostBuilder UsePlatformExternalJsonFilesConfiguration(this IWebHostBuilder builder, string path = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ConfigureAppConfiguration((hostingContext, configurationBuilder) =>
            {
                configurationBuilder.AddExternalJsonFiles(path);
            });

            return builder;
        }

        /// <summary>
        /// Add the azure key vault to the configuration sources.  The configuration settings are found in the
        /// "AzureKeyVault" section unless otherwise specified.
        /// </summary>
        /// <param name="builder">IWebHostBuilder to configure.</param>
        /// <param name="path">Alternate path to a configuration section.</param>
        /// <returns>Configured IWebHostBuilder.</returns>
        public static IWebHostBuilder UsePlatformAzureKeyVaultConfiguration(this IWebHostBuilder builder, string path = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ConfigureAppConfiguration((hostingContext, configurationBuilder) => configurationBuilder.AddAzureKeyVault());
            return builder;
        }

        /// <summary>
        /// Configures default logging.
        /// </summary>
        /// <param name="builder">IWebHostBuilder to configure.</param>
        /// <returns>Configured IWebHostBuilder.</returns>
        public static IWebHostBuilder UsePlatformLoggingDefaults(this IWebHostBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder;
        }

        private static IWebHostBuilder UseExosConfiguration(this IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                var configuration = configurationBuilder.Build();
                var url = ConfigurationHelper.GetAppConfigurationUrl(configuration);
                if (string.IsNullOrEmpty(url))
                {
                    return;
                }

                var credentials = ExosCredentials.GetDefaultCredential();

                // This will add an AzureAppConfigurationProvider to the configuration sources and will pull in all the
                // "ExosMacro:" tokens and values along side our existing appsettings.json values. We disable AKV secret
                // resolution at this stage by overriding the SecretResolve and handle secret resolution at the same time
                // we handle token replacement in service configuration.

                // The most important thing to understand here is that at this stage the configuration will include both:
                // unresolved appsettings.json data AND Azure App Configuration "ExosMacro:" data.

                configurationBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(new Uri(url), credentials)
                        .Select("ExosMacro:*")
                        .UseFeatureFlags(features =>
                        {
                            features.CacheExpirationInterval = TimeSpan.FromMinutes(5);
                            features.Select("Global:*");
                            features.Select($"{AssemblyHelper.EntryAssemblyName}:*");
                        })
                        .ConfigureKeyVault(kv =>
                        {
                            // Leave the AKV URL for later
                            kv.SetSecretResolver(u => ValueTask.FromResult(u.ToString()));
                        });
                });
            });

            return builder;
        }
    }
}
