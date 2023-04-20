namespace Exos.Platform.AspNetCore.AppConfiguration
{
    using System;
    using Azure.Core;
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;

    /// <summary>
    /// Options used to configure the behavior of an Azure App Configuration provider.
    /// </summary>
    [Obsolete("Applications should use IWebHostBuilder.UsePlatformConfigurationDefaults and IServiceCollection.AddExosPlatformDefaults.")]
    public sealed class ExosAzureAppConfigurationOptions
    {
        /// <summary>
        /// Gets the endpoint of the Azure App Configuration.
        /// If this property is set, the <see cref="ExosAzureAppConfigurationOptions.Credential" /> property also needs to be set.
        /// </summary>
        /// <value>
        /// The endpoint.
        /// </value>
        internal Uri Endpoint { get; private set; }

        /// <summary>
        /// Gets the connection string to use to connect to Azure App Configuration.
        /// If this property is set, the <see cref="ExosAzureAppConfigurationOptions.Endpoint" /> property also needs to be set.
        /// </summary>
        internal TokenCredential Credential { get; private set; }

        /// <summary>
        /// Connect the provider to Azure App Configuration using endpoint and token credentials.
        /// </summary>
        /// <param name="endpoint">The endpoint of the Azure App Configuration to connect to.</param>
        /// <param name="credential">Token credentials to use to connect.</param>
        public ExosAzureAppConfigurationOptions Connect(Uri endpoint, TokenCredential credential)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Credential = credential ?? throw new ArgumentNullException(nameof(credential));

            return this;
        }

        /// <summary>
        /// Specify what key-values to include in the configuration provider.
        /// <see cref="Select"/> can be called multiple times to include multiple sets of key-values.
        /// </summary>
        /// <param name="keyFilter">
        /// The key filter to apply when querying Azure App Configuration for key-values.
        /// The characters asterisk (*), comma (,) and backslash (\) are reserved and must be escaped using a backslash (\).
        /// Built-in key filter options: <see cref="KeyFilter"/>.
        /// </param>
        /// <param name="labelFilter">
        /// The label filter to apply when querying Azure App Configuration for key-values. By default the null label will be used. Built-in label filter options: <see cref="LabelFilter"/>
        /// The characters asterisk (*) and comma (,) are not supported. Backslash (\) character is reserved and must be escaped using another backslash (\).
        /// </param>
        public ExosAzureAppConfigurationOptions Select(string keyFilter, string labelFilter = LabelFilter.Null)
        {
            if (string.IsNullOrEmpty(keyFilter))
            {
                throw new ArgumentNullException(nameof(keyFilter));
            }

            if (labelFilter == null)
            {
                labelFilter = LabelFilter.Null;
            }

            // Do not support * and , for label filter for now.
            if (labelFilter.Contains('*', StringComparison.OrdinalIgnoreCase) || labelFilter.Contains(',', StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The characters '*' and ',' are not supported in label filters.", nameof(labelFilter));
            }

            return this;
        }
    }
}