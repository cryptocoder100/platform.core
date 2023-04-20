#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using Nito.AsyncEx;
    using StackExchange.Redis;

    // $/FieldServices/CommonLib/Dev4_EXOS_RC1/src/CommonLib/WebUtilities/Global.asax.cs
    // $/FieldServices/CommonLib/Dev4_EXOS_RC1/src/CommonCaching/CommonCacheProvider.cs

    /// <summary>
    /// Exos Authorization Token Authentication.
    /// </summary>
    public class ExosAuthTokenAuthenticationHandler : AuthenticationHandler<ExosAuthTokenAuthenticationOptions>
    {
        private static AsyncLazy<IDatabase> _cacheLazy;
        private readonly ILogger<ExosAuthTokenAuthenticationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosAuthTokenAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">ExosCookieAuthenticationOptions.</param>
        /// <param name="logger">ILoggerFactory.</param>
        /// <param name="encoder">UrlEncoder.</param>
        /// <param name="clock">ISystemClock.</param>
        public ExosAuthTokenAuthenticationHandler(IOptionsMonitor<ExosAuthTokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _logger = logger.CreateLogger<ExosAuthTokenAuthenticationHandler>();

            // We need instance member options to init Redis connection but don't want new Redis connection on each instance...
            // The best we can do in this circumstance is use instance options first time to create static connection.
            if (_cacheLazy == null)
            {
                _cacheLazy = new AsyncLazy<IDatabase>(async () =>
                {
                    var connection = await ConnectionMultiplexer.ConnectAsync(Options.ReadWriteConnectionString).ConfigureAwait(false);
                    var cache = connection.GetDatabase();

                    return cache;
                });
            }
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Look for the token header
            if (!Request.Headers.TryGetValue("authtoken", out StringValues authtoken) || StringValues.IsNullOrEmpty(authtoken))
            {
                // No auth token on request
                _logger.LogDebug("EXOS 'authtoken' header was not found or was empty.");
                return AuthenticateResult.NoResult();
            }

            // Look for the AppToken in cache
            var cacheKey = GetCacheKey(authtoken);

            CacheItem cacheItem = null;
            try
            {
                var cache = await _cacheLazy.ConfigureAwait(false);
                var result = await cache.StringGetAsync(cacheKey).ConfigureAwait(false);
                if (result.IsNullOrEmpty || !result.HasValue)
                {
                    _logger.LogWarning("EXOS 'authtoken' authentication failed because the token could not be found in cache or has expired.");
                    return AuthenticateResult.Fail("The token does not exist or has expired.");
                }

                cacheItem = Deserialize<CacheItem>(result);
                if (cacheItem.Expiration.ToUniversalTime() < Clock.UtcNow)
                {
                    _logger.LogWarning("EXOS 'authtoken' authentication failed because the token has expired.");
                    await cache.KeyDeleteAsync(cacheKey).ConfigureAwait(false);
                    return AuthenticateResult.Fail("The token has expired.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EXOS 'authtoken' API key authentication failed with an error. Message: {Message}", ex.Message);
                throw;
            }

            var appToken = (AppToken)cacheItem.Data;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, appToken.UserName),
            };

            // add consumer related work orders here from the token into the claims
            // this now becomes the part of user context for consumer kind of users
            // where we don't have proper multi tenancy.
            if (appToken.WorkOrderIds != null && appToken.WorkOrderIds.Count > 0)
            {
                appToken.WorkOrderIds.ForEach(w =>
                {
                    claims.Add(new Claim("claimworkorderid", w));
                });
            }

            claims.Add(new Claim(ClaimConstants.OriginalAuthSchemeName, Scheme.Name));
            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _logger.LogDebug("User '{Username}' has been authenticated.", claimsIdentity?.Name);

            var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        // $/FieldServices/CommonLib/Dev4_EXOS_RC1/src/CommonCaching/Helper.cs

        /// <summary>
        /// Deserialize object.
        /// </summary>
        /// <typeparam name="T">The element type to deserialize.</typeparam>
        /// <param name="serializedEntry">Serialized object.</param>
        /// <returns>Deserialized object.</returns>
        private static T Deserialize<T>(byte[] serializedEntry)
        {
            var formatter = new BinaryFormatter();
            formatter.Binder = new CommonCachingDeserializationBinder();
            using (var stream = new MemoryStream(serializedEntry))
            {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
#pragma warning disable CA2300 // Do not use insecure deserializer BinaryFormatter
                return (T)formatter.Deserialize(stream);
#pragma warning restore CA2300 // Do not use insecure deserializer BinaryFormatter
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }

        // $/FieldServices/SpaLogin/Dev4_EXOS_RC1/src/RestWebAPIs/Login.RestWebAPIs/Utilities/AppConfigs.cs

        /// <summary>
        /// Get a Cache Key for the given Token.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <returns>Cache key.</returns>
        private static string GetCacheKey(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            return "MTK_" + token.ToLowerInvariant();
        }

        /// <inheritdoc/>
        private class CommonCachingDeserializationBinder : SerializationBinder
        {
            /// <inheritdoc/>
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (assemblyName.StartsWith("CommonCaching,", StringComparison.OrdinalIgnoreCase) && typeName == "CommonCaching.CacheItem")
                {
                    return typeof(Models.CacheItem);
                }

                if (assemblyName.StartsWith("CommonLib,", StringComparison.OrdinalIgnoreCase) && typeName == "CommonLib.Context.AppToken")
                {
                    return typeof(Models.AppToken);
                }

                if (assemblyName.StartsWith("Exos.UserSvc,", StringComparison.OrdinalIgnoreCase) && typeName == "Exos.UserSvc.Models.CacheItem")
                {
                    return typeof(Models.CacheItem);
                }

                if (assemblyName.StartsWith("Exos.UserSvc,", StringComparison.OrdinalIgnoreCase) && typeName == "Exos.UserSvc.Models.AppToken")
                {
                    return typeof(Models.AppToken);
                }

                return Type.GetType($"{typeName}, {assemblyName}");
            }
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase
