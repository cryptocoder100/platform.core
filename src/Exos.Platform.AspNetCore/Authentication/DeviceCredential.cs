#pragma warning disable CA1822 // Mark members as static
namespace Exos.Platform.AspNetCore.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Microsoft.Identity.Client;

    /// <summary>
    /// A <see cref="TokenCredential"/> implementation which outputs a code in Console and Debug output, prompting the user to manually open a browser and
    /// to interactively authenticate and obtain an access token.
    /// This process will only be required to authenticate the user once, then will silently acquire access tokens through the users refresh token as long as it's valid.
    /// </summary>
    public class DeviceCredential : TokenCredential
    {
        // Currently this is piggybacking off the Azure CLI client ID, but needs to be switched once the Developer Sign On application is available
        private const string ClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        private static readonly Uri Authority = new Uri("https://login.microsoftonline.com/organizations/");

        private static readonly Lazy<IPublicClientApplication> PublicClient = new Lazy<IPublicClientApplication>(CreatePublicClient);

        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="scope">The scope.</param>
        public static async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var token = await GetTokenAsync(authority, resource, scope);
            return token.AccessToken;
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        public static Task<AuthenticationResult> GetTokenAsync(IEnumerable<string> scopes)
        {
            return GetTokensInternalAsync(scopes);
        }

        /// <summary>
        /// Gets the token asynchronous.  Exists to be called by MSIL Framework methods automatically.  Not intended for direct usage.
        /// </summary>
        /// <param name="authority">The authority.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="scope">The scope.</param>
        public static Task<AuthenticationResult> GetTokenAsync(string authority, string resource, string scope)
        {
            if (string.IsNullOrEmpty(scope))
            {
                scope = ".default";
            }

            var scopes = new[] { System.IO.Path.Combine(resource, scope) };
            return GetTokensInternalAsync(scopes);
        }

        /// <summary>
        /// Clears the token cache.
        /// </summary>
        public async Task ClearTokenCacheAsync()
        {
            var accounts = (await PublicClient.Value.GetAccountsAsync()).ToList();

            while (accounts.Any())
            {
                await PublicClient.Value.RemoveAsync(accounts.First());
                accounts = (await PublicClient.Value.GetAccountsAsync()).ToList();
            }
        }

        /// <inheritdoc/>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var token = await GetTokenAsync(requestContext.Scopes);
            return new AccessToken(token.AccessToken, token.ExpiresOn);
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var token = GetTokenAsync(requestContext.Scopes).Result;
            return new AccessToken(token.AccessToken, token.ExpiresOn);
        }

        private static async Task<AuthenticationResult> GetTokensInternalAsync(IEnumerable<string> scopes)
        {
            var accounts = await PublicClient.Value.GetAccountsAsync();

            AuthenticationResult result;

            // All AcquireToken* methods store the tokens in the cache, so check the cache first.
            try
            {
                result = await PublicClient.Value.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // No token found in the cache or AAD insists that a form interactive auth is required (e.g. the tenant admin turned on MFA)
                // If you want to provide a more complex user experience, check out ex.Classification.
                result = await AcquireByDeviceCodeAsync(PublicClient.Value, scopes);
            }

            return result;
        }

        private static async Task<AuthenticationResult> AcquireByDeviceCodeAsync(IPublicClientApplication pca, IEnumerable<string> scopes)
        {
            try
            {
                var result = await pca.AcquireTokenWithDeviceCode(
                    scopes,
                    deviceCodeResult =>
                    {
                        Console.WriteLine(deviceCodeResult.Message);
                        Debug.WriteLine(deviceCodeResult.Message);
                        return Task.FromResult(0);
                    }).ExecuteAsync();

                return result;
            }
            catch (Exception ex)
            {
                // TODO: Add logging.  MSAL has rich exception communication; expand to handle situations better.
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static IPublicClientApplication CreatePublicClient()
        {
            var app = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(Authority, false)
                .WithDefaultRedirectUri()
                .Build();

            // TODO: Instantiate Token Cache to reduce number of times developers must authenticate
            // TokenCacheHelper.EnableSerialization(app.UserTokenCache);

            return app;
        }
    }
}
#pragma warning restore CA1822 // Mark members as static