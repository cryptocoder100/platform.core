namespace Exos.Platform.AspNetCore.AppConfiguration
{
    using System;
    using Azure.Data.AppConfiguration;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Defines the <see cref="ExosAzureAppConfigurationSource" />.
    /// </summary>
    [Obsolete("Applications should use IWebHostBuilder.UsePlatformConfigurationDefaults and IServiceCollection.AddExosPlatformDefaults.")]
    internal class ExosAzureAppConfigurationSource : IConfigurationSource
    {
        private readonly Func<ExosAzureAppConfigurationOptions> _optionsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosAzureAppConfigurationSource"/> class.
        /// </summary>
        /// <param name="optionsInitializer">The optionsInitializer<see cref="Action{ExosAzureAppConfigurationOptions}"/>.</param>
        public ExosAzureAppConfigurationSource(Action<ExosAzureAppConfigurationOptions> optionsInitializer)
        {
            _optionsProvider = () =>
            {
                var configurationOptions = new ExosAzureAppConfigurationOptions();
                optionsInitializer(configurationOptions);
                return configurationOptions;
            };
        }

        /// <inheritdoc/>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            try
            {
                var options = _optionsProvider();

                if (!(options.Endpoint != null) || options.Credential == null)
                {
                    throw new ArgumentException("Please call AzureAppConfigurationOptions.Connect to specify how to connect to Azure App Configuration.");
                }

                var client = new ConfigurationClient(options.Endpoint, options.Credential, new ConfigurationClientOptions());

                var configurationProvider = new ExosAzureAppConfigurationProvider(client);
                return configurationProvider;
            }
            catch (InvalidOperationException ex)
            {
                throw new ArgumentException(ex.Message, ex);
            }
        }
    }
}
