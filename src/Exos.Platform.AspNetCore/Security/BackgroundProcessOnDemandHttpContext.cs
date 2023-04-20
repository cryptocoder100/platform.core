#pragma warning disable CA1001 // Types that own disposable fields should be disposable
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.KeyVault;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This simulates http user context for background processes.
    /// </summary>
    public class BackgroundProcessOnDemandHttpContext : IHttpContextAccessor, IOnDemandUserContext
    {
        private const string AuthHeaderName = "Authorization";
        private readonly IOptions<UserContextOptions> _options;
        private readonly IOptions<AzureKeyVaultSettings> _azureOptions;
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceProvider _serviceProvider;
        private readonly AzureKeyVaultKeyClient _keyVaultKeyClient;
        private SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProcessOnDemandHttpContext"/> class.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        /// <param name="optionsAccessor"><see cref="IOptions{UserContextOptions}"/>.</param>
        /// <param name="azureOptionsAccessor"><see cref="IOptions{AzureKeyVaultSettings}"/>.</param>
        /// <param name="distributedCache"><see cref="IDistributedCache"/>.</param>
        /// <param name="keyVaultKeyClient"><see cref="AzureKeyVaultKeyClient"/>.</param>
        /// <param name="memoryCache">memoryCache.</param>
        public BackgroundProcessOnDemandHttpContext(
            IServiceProvider serviceProvider,
            IOptions<UserContextOptions> optionsAccessor,
            IOptions<AzureKeyVaultSettings> azureOptionsAccessor,
            IDistributedCache distributedCache,
            AzureKeyVaultKeyClient keyVaultKeyClient,
            IMemoryCache memoryCache)
        {
            _options = optionsAccessor;
            _azureOptions = azureOptionsAccessor;
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _serviceProvider = serviceProvider;
            _keyVaultKeyClient = keyVaultKeyClient;
        }

        /// <summary>
        /// Gets or sets the HttpContext
        /// Gets Or sets HttpContext..
        /// </summary>
        public HttpContext HttpContext { get; set; }

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="userCtxOptions">userCtxOptions.</param>
        /// <returns>populates the user context.</returns>
        public async Task Init(UserInitOptions userCtxOptions)
        {
            if (userCtxOptions == null)
            {
                throw new ArgumentNullException(nameof(userCtxOptions));
            }

            await _initSemaphore.WaitAsync();

            try
            {
                var context = new DefaultHttpContext();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userCtxOptions.UserName),
                    new Claim(ClaimConstants.OriginalAuthSchemeName, PlatformAuthScheme.Bearer.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, userCtxOptions.AuthSchemeName);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                if (userCtxOptions.KeyValuePairs != null && userCtxOptions.KeyValuePairs.Count > 0)
                {
                    foreach (var kv in userCtxOptions.KeyValuePairs)
                    {
                        context.Request.Headers.Add(kv.Key, kv.Value);

                        // Push Headers into Activity.Current as well. This is used by Background processes.
                        if (Activity.Current != null && Activity.Current.Tags != null)
                        {
                            Activity.Current.SetKeyValues(new Dictionary<string, string> { { kv.Key, kv.Value } });
                        }
                    }
                }

                context.User = claimsPrincipal;

                var appTokenProvider = _serviceProvider.GetService<IAppTokenProvider>();

                var accessToken = await appTokenProvider.GetToken(userCtxOptions.UserName, userCtxOptions.Password);

                if (accessToken != null)
                {
                    if (context.Request.Headers.ContainsKey(AuthHeaderName))
                    {
                        context.Request.Headers.Remove(AuthHeaderName);
                    }

                    context.Request.Headers.Add(AuthHeaderName, $"bearer {accessToken}");
                }

                var logger = _serviceProvider.GetService<ILogger<UserContextMiddleware>>();
                var clientFactory = _serviceProvider.GetService<IHttpClientFactory>();

                HttpContext = context;

                var userContextMiddleware = new UserContextMiddleware(null, logger, _options, _distributedCache, clientFactory, _keyVaultKeyClient, _memoryCache);
                await userContextMiddleware.Invoke(HttpContext).ConfigureAwait(false);
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
    }
}
#pragma warning restore CA1001 // Types that own disposable fields should be disposable