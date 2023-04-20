namespace Exos.Platform.AspNetCore.Authentication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Microsoft.Azure.Services.AppAuthentication;

    /// <summary>
    /// Class which provides a fallback mechanism of <see cref="DeviceCredential"/> if other providers in AzureServiceTokenProvider fail to authenticate.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider" />
    public class ExosAzureServiceTokenProvider
        : AzureServiceTokenProvider
    {
        /// <inheritdoc/>
        public override async Task<string> GetAccessTokenAsync(string resource, string tenantId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExosCredentials.GetAzureServiceTokenAsync(resource, cancellationToken);
            }
            catch (AuthenticationFailedException)
            {
                return await DeviceCredential.GetAccessTokenAsync(string.Empty, resource, string.Empty);
            }
        }
    }
}
