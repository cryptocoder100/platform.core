namespace Exos.Platform.AspNetCore.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Provides mechanisms to generate MSI tokens which facilitate access to Azure resources.
    /// </summary>
    public static class ExosCredentials
    {
        // private const string MediaServiceResource = "https://management.azure.com";
        private const string ManagedIdentityVariableName = "EXOS_MANAGED_IDENTITY_APP_ID";
        private const string KubernetesHostedVariableName = "EXOS_IN_KUBERNETES_ENVIRONMENT";

        /// <summary>
        /// Get the DefaultCredential.
        /// </summary>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <returns>The <see cref="TokenCredential"/>.</returns>
        public static TokenCredential GetDefaultCredential(IConfiguration configuration = null)
        {
            var credentials = new List<TokenCredential>
            {
                new EnvironmentCredential(),
                new ManagedIdentityCredential(GetManagedClientId(configuration)),
            };

            var inKubernetesFlag = Environment.GetEnvironmentVariable(KubernetesHostedVariableName);
            if (inKubernetesFlag == null || inKubernetesFlag.Equals("true", StringComparison.OrdinalIgnoreCase) == false)
            {
                credentials.Add(new VisualStudioCredential());
                credentials.Add(new VisualStudioCodeCredential());
                credentials.Add(new InteractiveBrowserCredential());
                credentials.Add(new DeviceCredential());
            }

            var credential = new ChainedTokenCredential(credentials.ToArray());
            return credential;
        }

        /// <summary>
        /// Gets a Azure service token.
        /// </summary>
        /// <param name="resource">Azure Resoruce for which Token is required.</param>
        /// <param name="cancellationToken">cancellationToken.</param>
        public static Task<string> GetAzureServiceTokenAsync(string resource, CancellationToken cancellationToken = default) => GetServiceToken(resource, cancellationToken);

        /// <summary>
        /// GetManagedClientId Either read from configuration or from envt variable.
        /// </summary>
        /// <param name="configuration">Configuration params.</param>
        /// <returns>Managed Client Id.</returns>
        internal static string GetManagedClientId(IConfiguration configuration = null)
        {
            var managedClientId = configuration != null ?
                configuration[ManagedIdentityVariableName] :
                Environment.GetEnvironmentVariable(ManagedIdentityVariableName);
            return managedClientId;
        }

        /// <summary>
        /// Gets a service token.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private static async Task<string> GetServiceToken(string resource, CancellationToken cancellationToken = default)
        {
            // Note: Standard away to do this is new AzureServiceTokenProvider().GetAccessTokenAsync(...);
            // We cannot use as, at this time, that class doesn't support specifying Managed Client ID which we require.
            var defaultCredential = GetDefaultCredential();
            var scope = GetDefaultScope(new Uri(resource));
            var token = await defaultCredential.GetTokenAsync(new TokenRequestContext(new[] { scope }, Guid.NewGuid().ToString()), cancellationToken);
            return token.Token;
        }

        // Borrowed from Azure.Data.AppConfiguration.ConfigurationClient.
        private static string GetDefaultScope(Uri uri)
            => $"{uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped)}/.default";
    }
}
