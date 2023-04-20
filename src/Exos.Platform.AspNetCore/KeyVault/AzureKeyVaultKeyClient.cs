namespace Exos.Platform.AspNetCore.KeyVault
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Security.KeyVault.Keys;
    using Azure.Security.KeyVault.Keys.Cryptography;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Middleware;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Enable access to Azure Key Vault Keys.
    /// </summary>
    public class AzureKeyVaultKeyClient
    {
        private readonly ILogger<AzureKeyVaultKeyClient> _logger;
        private readonly AzureKeyVaultSettings _azureKeyVaultSettings;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultKeyClient"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{AzureKeyVaultKeyClient}"/>.</param>
        /// <param name="azureKeyVaultSettings">The azureKeyVaultSettings<see cref="IOptions{AzureKeyVaultSettings}"/>.</param>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <param name="memoryCache">The memoryCache<see cref="IMemoryCache"/>.</param>
        public AzureKeyVaultKeyClient(
            ILogger<AzureKeyVaultKeyClient> logger,
            IOptions<AzureKeyVaultSettings> azureKeyVaultSettings,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _azureKeyVaultSettings = azureKeyVaultSettings?.Value ?? throw new ArgumentNullException(nameof(azureKeyVaultSettings));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <summary>
        /// Gets or sets the KeyVaultKeyClient.
        /// </summary>
        public KeyClient KeyVaultKeyClient { get; set; }

        /// <summary>
        /// Gets or sets the CurrentCredential.
        /// </summary>
        private TokenCredential CurrentCredential { get; set; }

        /// <summary>
        /// Get the CryptographyClient instance.
        /// </summary>
        /// <param name="keyName">The keyName<see cref="string"/>.</param>
        /// <returns>The <see cref="CryptographyClient"/>.</returns>
        public CryptographyClient GetCryptographyClient(string keyName)
        {
            KeyVaultKey keyVaultKey = GetKeyValue(keyName);
            if (!_memoryCache.TryGetValue(keyVaultKey.Id, out CryptographyClient cryptographyClient))
            {
                cryptographyClient = new CryptographyClient(keyVaultKey.Id, CurrentCredential);

                // Add CryptographyClient to memory cache
                double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                var cacheExpirationOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                };
                _memoryCache.Set(keyVaultKey.Id, cryptographyClient, cacheExpirationOptions);
            }

            return cryptographyClient;
        }

        /// <summary>
        /// Get the CryptographyClient instance.
        /// </summary>
        /// <param name="keyUri">The key uri <see cref="Uri"/>.</param>
        /// <returns>The <see cref="CryptographyClient"/>.</returns>
        public CryptographyClient GetCryptographyClient(Uri keyUri)
        {
            if (keyUri == null)
            {
                throw new ArgumentNullException(nameof(keyUri));
            }

            if (CurrentCredential == null)
            {
                CurrentCredential = ExosCredentials.GetDefaultCredential(_configuration);
            }

            if (!_memoryCache.TryGetValue(keyUri.AbsoluteUri, out CryptographyClient cryptographyClient))
            {
                cryptographyClient = new CryptographyClient(keyUri, CurrentCredential);

                // Add CryptographyClient to memory cache
                double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                var cacheExpirationOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                };
                _memoryCache.Set(keyUri.AbsoluteUri, cryptographyClient, cacheExpirationOptions);
            }

            return cryptographyClient;
        }

        /// <summary>
        /// Get the Key value from the azure key vault.
        /// </summary>
        /// <param name="keyName">Key Name.</param>
        /// <param name="version">The version of the Key.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The KeyVaultKey Value.</returns>
        public KeyVaultKey GetKeyValue(string keyName, string version = null, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            if (!_memoryCache.TryGetValue(keyName, out Response<KeyVaultKey> keyVaultKey))
            {
                try
                {
                    if (keyVaultUrl != null)
                    {
                        SetKeyClient(keyVaultUrl);
                    }
                    else
                    {
                        SetKeyClient();
                    }

                    _logger.LogDebug($"Retrieving key {LoggerHelper.SanitizeValue(keyName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                    keyVaultKey = KeyVaultKeyClient.GetKey(keyName, version, cancellationToken);
                    _logger.LogDebug($"Retrieved key {LoggerHelper.SanitizeValue(keyName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}. Version {LoggerHelper.SanitizeValue(keyVaultKey.Value.Properties.Version)}");

                    // Add KeyVaultKey to memory cache
                    double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(keyName, keyVaultKey, cacheExpirationOptions);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                    throw new NotFoundException($"Key Name: {keyName}", $"{ex.Status} - Service request failed, unable to get specified key.", ex);
                }
            }

            return keyVaultKey.Value;
        }

        /// <summary>
        /// Get the Key value from the azure key vault.
        /// </summary>
        /// <param name="keyName">Key Name.</param>
        /// <param name="version">The version of the Key.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The KeyVaultKey Value.</returns>
        public async Task<KeyVaultKey> GetKeyValueAsync(string keyName, string version = null, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            if (!_memoryCache.TryGetValue(keyName, out Response<KeyVaultKey> keyVaultKey))
            {
                try
                {
                    if (keyVaultUrl != null)
                    {
                        SetKeyClient(keyVaultUrl);
                    }
                    else
                    {
                        SetKeyClient();
                    }

                    _logger.LogDebug($"Retrieving key {LoggerHelper.SanitizeValue(keyName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                    keyVaultKey = await KeyVaultKeyClient.GetKeyAsync(keyName, version, cancellationToken);
                    _logger.LogDebug($"Retrieved key {LoggerHelper.SanitizeValue(keyName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}. Version {LoggerHelper.SanitizeValue(keyVaultKey.Value.Properties.Version)}");

                    // Add KeyVaultKey to memory cache
                    double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(keyName, keyVaultKey, cacheExpirationOptions);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                    throw new NotFoundException($"Key Name: {keyName}", $"{ex.Status} - Service request failed, unable to get specified key.", ex);
                }
            }

            return keyVaultKey.Value;
        }

        private KeyClientOptions GetKeyClientOptions()
        {
            KeyClientOptions keyClientOptions = new KeyClientOptions();
            keyClientOptions.Retry.Mode = RetryMode.Exponential;
            keyClientOptions.Retry.MaxRetries = _azureKeyVaultSettings.MaxRetries;
            keyClientOptions.Retry.Delay = TimeSpan.FromSeconds(_azureKeyVaultSettings.RetryDelay);
            keyClientOptions.Retry.MaxDelay = TimeSpan.FromSeconds(_azureKeyVaultSettings.MaxDelay);
            return keyClientOptions;
        }

        private void SetKeyClient()
        {
            if (_azureKeyVaultSettings.AuthenticationType == AzureKeyVaultAuthenticationType.MSI)
            {
                if (_azureKeyVaultSettings.Url != null)
                {
                    if (CurrentCredential == null)
                    {
                        CurrentCredential = ExosCredentials.GetDefaultCredential(_configuration);
                    }

                    KeyVaultKeyClient = new KeyClient(_azureKeyVaultSettings.Url, CurrentCredential, GetKeyClientOptions());
                }
                else
                {
                    throw new ArgumentException("Provide a Key Vault Url in appsettings for MSI authentication.");
                }
            }
            else
            {
                throw new ArgumentException("Key Value Key client needs  MSI authentication.");
            }
        }

        private void SetKeyClient(Uri keyVaultUrl)
        {
            if (CurrentCredential == null)
            {
                CurrentCredential = ExosCredentials.GetDefaultCredential(_configuration);
            }

            // Set the keyvault using MSI
            KeyVaultKeyClient = new KeyClient(keyVaultUrl, CurrentCredential, GetKeyClientOptions());
        }
    }
}
