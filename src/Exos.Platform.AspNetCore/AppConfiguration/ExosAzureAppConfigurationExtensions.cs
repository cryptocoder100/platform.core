namespace Exos.Platform.AspNetCore.AppConfiguration
{
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Extension methods for IConfigurationBuilder.
    /// </summary>
    public static class ExosAzureAppConfigurationExtensions
    {
        /// <summary>
        /// Adds the exos azure application configuration.
        /// </summary>
        /// <param name="configurationBuilder">The configurationBuilder<see cref="IConfigurationBuilder"/>.</param>
        /// <param name="action">The action<see cref="Action{ExosAzureAppConfigurationOptions}"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        [Obsolete("Applications should use IWebHostBuilder.UsePlatformConfigurationDefaults and IServiceCollection.AddExosPlatformDefaults.")]
        public static IConfigurationBuilder AddExosAzureAppConfiguration(this IConfigurationBuilder configurationBuilder, Action<ExosAzureAppConfigurationOptions> action)
        {
            _ = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
            return configurationBuilder.Add(new ExosAzureAppConfigurationSource(action));
        }
    }
}
