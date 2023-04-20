namespace Exos.Platform.AspNetCore.AppConfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.AppConfiguration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;

    /// <summary>
    /// Defines the <see cref="ExosAzureAppConfigurationProvider" />.
    /// </summary>
    [Obsolete("Applications should use IWebHostBuilder.UsePlatformConfigurationDefaults and IServiceCollection.AddExosPlatformDefaults.")]
    internal class ExosAzureAppConfigurationProvider : ConfigurationProvider
    {
        private const string ExosKeyPrefix = "Exos:";
        private readonly ConfigurationClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosAzureAppConfigurationProvider"/> class.
        /// </summary>
        /// <param name="client">The client<see cref="ConfigurationClient"/>.</param>
        public ExosAzureAppConfigurationProvider(ConfigurationClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public override void Load()
        {
            LoadAll().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task LoadAll()
        {
            var data = new Dictionary<string, ConfigurationSetting>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Load all key-values with the null label.
                var selector = new SettingSelector
                {
                    KeyFilter = KeyFilter.Any,
                    LabelFilter = LabelFilter.Null
                };

                await foreach (var setting in _client.GetConfigurationSettingsAsync(selector, CancellationToken.None).ConfigureAwait(false))
                {
                    data[setting.Key] = setting;
                }
            }
            catch (Exception exception) when (exception is KeyVaultReferenceException ||
                                              exception is RequestFailedException ||
                                              ((exception as AggregateException)?.InnerExceptions?.All(e => e is RequestFailedException) ?? false) ||
                                              exception is OperationCanceledException)
            {
                throw;
            }

            SetData(data);
        }

        private void SetData(IDictionary<string, ConfigurationSetting> data)
        {
            // Set the application data for the configuration provider
            var applicationData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (_, value) in data)
            {
                var key = value.Key;

                if (key.StartsWith(ExosKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                applicationData[key] = value.Value;
            }

            Data = applicationData;

            // Notify that the configuration has been updated
            OnReload();
        }
    }
}
