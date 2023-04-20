namespace Exos.Platform.AspNetCore.Authentication
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Identity.Client;

    /// <summary>
    /// This class will behave as an interface for getting token for standalone services.
    /// Standalone applications such as listeners can get the instance and gall the Getb2cToken method to obtain a token.
    /// </summary>
    public class B2CAppTokenProvider : IAppTokenProvider
    {
        private readonly ConcurrentDictionary<string, AuthenticationResult> _tokens;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="B2CAppTokenProvider"/> class.
        /// </summary>
        /// <param name="configuration">Configuration from dependency injection. .</param>
        public B2CAppTokenProvider(IConfiguration configuration)
        {
            _tokens = new ConcurrentDictionary<string, AuthenticationResult>();
            _configuration = configuration;
        }

        /// <summary>
        /// This method will obtain the access token from based on the client application.
        /// </summary>
        /// <param name="clientId">ClientId from the b2c application.</param>
        /// <param name="clientSecret">ClientSecret from the b2C application.</param>
        /// <returns>A <see cref="Task{TResult}"/> Returns the access token.</returns>
        public async Task<string> GetToken(string clientId, string clientSecret)
        {
            return await GetToken(clientId, clientSecret, false).ConfigureAwait(false);
        }

        /// <summary>
        /// This method will obtain the access token from based on the client application.
        /// </summary>
        /// <param name="clientId">ClientId from the b2c application.</param>
        /// <param name="clientSecret">ClientSecret from the b2C application.</param>
        /// <param name="generateNewToken">pass parameter to force the generate token.</param>
        /// <returns>A <see cref="Task{TResult}"/> Returns the access token.</returns>
        public async Task<string> GetToken(string clientId, string clientSecret, bool generateNewToken)
        {
            // Even if this is a console application here, a daemon application is a confidential client application
            string authority = string.Format(
                CultureInfo.InvariantCulture,
                _configuration.GetValue<string>("B2CApp:Instance"),
                _configuration.GetValue<string>("B2CApp:Tenant"));
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri(authority))
                    .Build();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default",
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator

            string[] scopes = { string.Format(CultureInfo.InvariantCulture, _configuration.GetValue<string>("B2CApp:Scope"), clientId) };
            if (!generateNewToken && _tokens.TryGetValue(clientId, out var existingToken) && IsTokenValid(existingToken))
            {
                return existingToken.AccessToken;
            }

            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            _tokens.AddOrUpdate(clientId, result, (clientId, existingToken) => result);
            return result.AccessToken;
        }

        /// <inheritdoc />
        public string GetAdditionalUserContext(string userName, string email, string expirationTicks)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }

            if (string.IsNullOrEmpty(expirationTicks))
            {
                throw new ArgumentNullException(nameof(expirationTicks));
            }

            var baseDetails = $"{userName}|{email}|{expirationTicks}";

            var detailHash = Security.ApiKeySecurity.Version1.HashPassword(baseDetails);
            baseDetails += $"~{detailHash}";

            return baseDetails;
        }

        /// <summary>
        /// The IsTokenValid.
        /// </summary>
        /// <param name="token">The token<see cref="AuthenticationResult"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private static bool IsTokenValid(AuthenticationResult token)
        {
            // if the current utc time +2 minute is still within the exipiry , then the token is valid
            if (token.ExpiresOn > DateTime.UtcNow.AddMinutes(20))
            {
                return true;
            }

            return false;
        }
    }
}
