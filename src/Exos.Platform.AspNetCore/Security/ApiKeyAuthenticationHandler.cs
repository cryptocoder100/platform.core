#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using Nito.AsyncEx;
    using StackExchange.Redis;

    /// <summary>
    /// Authentication Handler using API key.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private static readonly Regex AuthorizationHeaderRegex = new Regex(@"^(\w+)\s+(.*)", RegexOptions.IgnoreCase);
        private static HttpClient _userClient;
        private static AsyncLazy<IDatabase> _cacheLazy;
        private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
        private readonly IAppTokenProvider _appTokenProvider;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler" /> class.
        /// </summary>
        /// <param name="options">ApiKeyAuthenticationOptions.</param>
        /// <param name="logger">ILoggerFactory.</param>
        /// <param name="urlEncoder">UrlEncoder.</param>
        /// <param name="clock">ISystemClock.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="appTokenProvider">IAppTokenProvider.</param>
        /// <param name="configuration">IConfiguration.</param>
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder urlEncoder,
            ISystemClock clock,
            IHttpClientFactory httpClientFactory,
            IAppTokenProvider appTokenProvider,
            IConfiguration configuration)
            : base(options, logger, urlEncoder, clock)
        {
            _appTokenProvider = appTokenProvider;
            _configuration = configuration;
            _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
            if (_userClient == null && httpClientFactory != null)
            {
                _userClient = httpClientFactory.CreateClient("ExosNative");
            }

            // We need instance member options to init Redis connection but don't want new Redis connection on each instance...
            // The best we can do in this circumstance is use instance options first time to create static connection.
            if (_cacheLazy == null)
            {
                _cacheLazy = new AsyncLazy<IDatabase>(async () =>
                {
                    var connection = await ConnectionMultiplexer.ConnectAsync(Options.RedisConfiguration).ConfigureAwait(false);
                    var cache = connection.GetDatabase();
                    return cache;
                });
            }
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Extract the authorization header
            if (!Request.Headers.TryGetValue("Authorization", out StringValues header) || StringValues.IsNullOrEmpty(header))
            {
                // Not an authenticated request
                _logger.LogDebug("The 'Authorization' header was not found or was empty.");
                return AuthenticateResult.NoResult();
            }

            var match = AuthorizationHeaderRegex.Match(header);
            if (!match.Success || Options.AuthorizationScheme != match.Groups[1].Value || !Regex.IsMatch(match.Groups[2].Value, Options.ApiKeyPattern))
            {
                // Not a valid API key or means something else
                _logger.LogDebug("The 'Authorization' header did not match scheme or expected format.");
                return AuthenticateResult.NoResult();
            }

            UserModel userModelFromDbOrCache;

            string accessToken;
            try
            {
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[2].Value));
                var credentialsParts = credentials.Split(':');
                var usernameFromHeader = credentialsParts[0];
                var passwordFromHeader = credentialsParts[1];

                userModelFromDbOrCache = await GetCachedUserAsync(usernameFromHeader).ConfigureAwait(false);
                if (userModelFromDbOrCache == null)
                {
                    _logger.LogError("API key authentication failed because of an invalid username.");
                    return AuthenticateResult.Fail("Invalid username or password.");
                }

                if (!string.IsNullOrEmpty(userModelFromDbOrCache.ClientSecret))
                {
                    // Validate the client secret
                    var apiKeyVersion = ApiKeySecurity.DetectVersion(userModelFromDbOrCache.ClientSecret);
                    var apiKeySecurity = ApiKeySecurity.FromVersion(apiKeyVersion);

                    if (!apiKeySecurity.ValidatePassword(passwordFromHeader, userModelFromDbOrCache.ClientSecret))
                    {
                        _logger.LogError("API key authentication failed because of an invalid credential.");
                        return AuthenticateResult.Fail("Invalid username or password.");
                    }
                }
                else
                {
                    // If ClientSecret is null in userModel check in appsettings for username/secret (username/secret is stored in keyvault).
                    var secretForUserName = GetSecretForUserName(usernameFromHeader);
                    if (!passwordFromHeader.Equals(secretForUserName.ClientSecret, StringComparison.Ordinal))
                    {
                        _logger.LogError("API key authentication failed because of an invalid credential.");
                        return AuthenticateResult.Fail("Invalid username or password.");
                    }
                }

                var clientIdAndSecret = GetClientIdAndSecretForUser(usernameFromHeader);

                accessToken = await _appTokenProvider.GetToken(clientIdAndSecret.ClientId, clientIdAndSecret.ClientSecret);
                if (accessToken == null)
                {
                    _logger.LogDebug("API key authentication failed to get token.");
                    return AuthenticateResult.Fail("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API key authentication failed with an error. Message: {Message}", ex.Message);
                throw;
            }

            // Build the claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userModelFromDbOrCache.Username),
                new Claim(ClaimConstants.OriginalAuthSchemeName, Scheme.Name),
                new Claim(ClaimConstants.ApiKeyAccessToken, accessToken),
            };
            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _logger.LogDebug("User '{Username}' has been authenticated.", claimsIdentity.Name);

            var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        private ApiKeyClientIdAndSecret GetClientIdAndSecretForUser(string usernameFromHeader)
        {
            var keyNameForClientId = $"{usernameFromHeader}-ClientId";
            var keyNameForClientSecret = $"{usernameFromHeader}-ClientSecret";

            var userKeyConfiguration = _configuration.GetSection("ApiKeyMapping");

            var result = new ApiKeyClientIdAndSecret
            {
                ClientId = userKeyConfiguration[keyNameForClientId],
                ClientSecret = userKeyConfiguration[keyNameForClientSecret]
            };

            return result;
        }

        private ApiKeyClientIdAndSecret GetSecretForUserName(string usernameFromHeader)
        {
            var keyNameForUserName = $"{usernameFromHeader}-UserName";
            var keyNameForUserSecret = $"{usernameFromHeader}-Secret";

            var userKeyConfiguration = _configuration.GetSection("ApiKeyMapping");

            var result = new ApiKeyClientIdAndSecret
            {
                ClientId = userKeyConfiguration[keyNameForUserName],
                ClientSecret = userKeyConfiguration[keyNameForUserSecret]
            };

            return result;
        }

        /// <summary>
        /// Get User from Cache.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <returns>UserModel that represent an user.</returns>
        private async Task<UserModel> GetCachedUserAsync(string username)
        {
            // No cache?
            if (Options.CacheDuration == null || Options.CacheDuration <= 0)
            {
                return await GetUserAsync(username).ConfigureAwait(false);
            }

            UserModel user;
            RedisValue result;
            IDatabase cache;

            var cacheKey = $"{nameof(ApiKeyAuthenticationHandler)}:{username.Trim().ToLowerInvariant()}";

            // Get from cache
            try
            {
                cache = await _cacheLazy.ConfigureAwait(false);
                result = await cache.StringGetAsync(cacheKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting user from Redis cache. Exception handled as cache miss.");
                result = RedisValue.Null;
                cache = null;
            }

            if (result.IsNullOrEmpty || !result.HasValue)
            {
                // Call user service
                user = await GetUserAsync(username).ConfigureAwait(false);

                // Store in cache
                if (user != null && cache != null)
                {
                    try
                    {
                        await cache.StringSetAsync(
                            cacheKey,
                            JsonSerializer.Serialize(user),
                            expiry: TimeSpan.FromSeconds((int)Options.CacheDuration)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error caching user in Redis. Exception ignored.");
                    }
                }
            }
            else
            {
                // Used cached value
                user = JsonSerializer.Deserialize<UserModel>(result);
            }

            return user;
        }

        /// <summary>
        /// Get User details by user name calling User Service.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <returns>UserModel.</returns>
        private async Task<UserModel> GetUserAsync(string username)
        {
            var query = new Dictionary<string, string>
            {
                { "username", username },
            };

            var uri = new Uri(Options.UserSvc + QueryHelpers.AddQueryString("/api/v1/users/internal", query));

            try
            {
                using (var response = await _userClient.GetAsync(uri).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var list = await response.Content.ReadAsJsonAsync<ListModel<UserModel>>().ConfigureAwait(false);

                    if (list.Data.Count > 0)
                    {
                        return list.Data[0];
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and add some more data
                ex = new HttpRequestException($"An error occurred while sending the request. Uri: '{uri}', Method: 'GET'.", ex);
                _logger.LogWarning(ex, "There was an error calling the UserSvc for username lookup. Uri: '{uri}', Method: '{method}'", uri, "GET");
                throw ex;
            }

            return null;
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase