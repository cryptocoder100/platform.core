namespace Exos.Platform.AspNetCore.KeyVault
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Middleware;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    /// <summary>
    /// Enable access to Azure Key Vault Secrets.
    /// </summary>
    public class AzureKeyVaultSecretClient
    {
        private readonly ILogger<AzureKeyVaultSecretClient> _logger;
        private readonly AzureKeyVaultSettings _azureKeyVaultSettings;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultSecretClient"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="azureKeyVaultSettings"><see cref="AzureKeyVaultSettings"/>.</param>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <param name="memoryCache"><see cref="IMemoryCache"/>.</param>
        public AzureKeyVaultSecretClient(
            ILogger<AzureKeyVaultSecretClient> logger,
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
        /// Gets or sets the Key Vault Secret Client.
        /// </summary>
        public SecretClient KeyVaultSecretClient { get; set; }

        private TokenCredential CurrentCredential { get; set; }

        /// <summary>
        /// Get the secret value from the azure key vault.
        /// </summary>
        /// <param name="secretName">Secret Name.</param>
        /// <param name="version">The version of the secret.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>Secret Value.</returns>
        public string GetSecretValue(string secretName, string version = null, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            if (!_memoryCache.TryGetValue(secretName, out KeyVaultSecret keyVaultSecret))
            {
                try
                {
                    // Get the secret value
                    if (keyVaultUrl != null)
                    {
                        SetSecretClient(keyVaultUrl);
                    }
                    else
                    {
                        SetSecretClient();
                    }

                    _logger.LogDebug($"Retrieving secret {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                    keyVaultSecret = KeyVaultSecretClient.GetSecret(secretName, version, cancellationToken);
                    _logger.LogDebug($"Retrieved secret {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}. Version {LoggerHelper.SanitizeValue(keyVaultSecret.Properties.Version)}.");

                    // Add KeyVaultSecret to memory cache
                    double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(secretName, keyVaultSecret, cacheExpirationOptions);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                    throw new NotFoundException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to get specified secret.", ex);
                }
            }

            return keyVaultSecret.Value;
        }

        /// <summary>
        /// Get the secret value from the azure key vault.
        /// </summary>
        /// <param name="secretName">Secret Name.</param>
        /// <param name="version">The version of the secret.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>Secret Value.</returns>
        public async Task<string> GetSecretValueAsync(string secretName, string version = null, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            if (!_memoryCache.TryGetValue(secretName, out KeyVaultSecret keyVaultSecret))
            {
                try
                {
                    // Get the secret value
                    if (keyVaultUrl != null)
                    {
                        SetSecretClient(keyVaultUrl);
                    }
                    else
                    {
                        SetSecretClient();
                    }

                    _logger.LogDebug($"Retrieving secret {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                    keyVaultSecret = await KeyVaultSecretClient.GetSecretAsync(secretName, version, cancellationToken);
                    _logger.LogDebug($"Retrieved secret {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}. Version {LoggerHelper.SanitizeValue(keyVaultSecret.Properties.Version)}.");

                    // Add KeyVaultSecret to memory cache
                    double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(secretName, keyVaultSecret, cacheExpirationOptions);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                    throw new NotFoundException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to get specified secret.", ex);
                }
            }

            return keyVaultSecret.Value;
        }

        /// <summary>
        /// Get the default secret value from the azure key vault.
        /// </summary>
        /// <param name="secretName">Secret Name.</param>
        /// <param name="version">The version of the secret.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The Secret Value and version.</returns>
        public string GetSecretValue(string secretName, out string version, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            if (!_memoryCache.TryGetValue(secretName, out KeyVaultSecret keyVaultSecret))
            {
                try
                {
                    // Get the secret value
                    if (keyVaultUrl != null)
                    {
                        SetSecretClient(keyVaultUrl);
                    }
                    else
                    {
                        SetSecretClient();
                    }

                    _logger.LogDebug($"Retrieving secret {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                    keyVaultSecret = KeyVaultSecretClient.GetSecret(secretName, null, cancellationToken);
                    version = keyVaultSecret.Properties.Version;
                    _logger.LogDebug($"Retrieved secret {LoggerHelper.SanitizeValue(secretName)}. Version {LoggerHelper.SanitizeValue(keyVaultSecret.Properties.Version)}.");

                    // Add KeyVaultSecret to memory cache
                    double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(secretName, keyVaultSecret, cacheExpirationOptions);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                    throw new NotFoundException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to get specified secret.", ex);
                }
            }

            version = keyVaultSecret.Properties.Version;
            return keyVaultSecret.Value;
        }

        /// <summary>
        /// Get the default secret value from the azure key vault.
        /// </summary>
        /// <param name="secretName">Secret Name.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The Secret Value and version.</returns>
        public async Task<(string KeyVaultSecretVersion, string KeyVaultSecretValue)> GetSecretValueAsync(string secretName, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            if (!_memoryCache.TryGetValue(secretName, out KeyVaultSecret keyVaultSecret))
            {
                try
                {
                    // Get the secret value
                    if (keyVaultUrl != null)
                    {
                        SetSecretClient(keyVaultUrl);
                    }
                    else
                    {
                        SetSecretClient();
                    }

                    _logger.LogDebug($"Retrieving secret {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                    keyVaultSecret = await KeyVaultSecretClient.GetSecretAsync(secretName, null, cancellationToken);
                    _logger.LogDebug($"Retrieved secret {LoggerHelper.SanitizeValue(secretName)}. Version {LoggerHelper.SanitizeValue(keyVaultSecret.Properties.Version)}.");

                    // Add KeyVaultSecret to memory cache
                    double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(secretName, keyVaultSecret, cacheExpirationOptions);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                    throw new NotFoundException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to get specified secret.", ex);
                }
            }

            return (keyVaultSecret.Properties.Version, keyVaultSecret.Value);
        }

        /// <summary>
        /// Get the versions of the secret.
        /// </summary>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The versions of the secret, First version is the current version.</returns>
        public List<string> GetSecretVersions(string secretName, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (keyVaultUrl != null)
                {
                    SetSecretClient(keyVaultUrl);
                }
                else
                {
                    SetSecretClient();
                }

                _logger.LogDebug($"Retrieving secret properties for {LoggerHelper.SanitizeValue(secretName)} from key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                var secretProperties = KeyVaultSecretClient.GetPropertiesOfSecretVersions(secretName, cancellationToken);
                _logger.LogDebug($"Retrieved secret properties for {LoggerHelper.SanitizeValue(secretName)}.");
                List<string> secretVersions = new List<string>();
                foreach (SecretProperties secretProperty in secretProperties)
                {
                    secretVersions.Add(secretProperty.Version);
                }

                return secretVersions;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                throw new NotFoundException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to get specified secret.", ex);
            }
        }

        /// <summary>
        /// Create a secret in azure key vault.
        /// </summary>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="secretValue">The value of the secret.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The version of the secret if secret is created successfully.</returns>
        public string CreateSecret(string secretName, string secretValue, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (keyVaultUrl != null)
                {
                    SetSecretClient(keyVaultUrl);
                }
                else
                {
                    SetSecretClient();
                }

                KeyVaultSecret keyVaultSecret = KeyVaultSecretClient.SetSecret(secretName, secretValue, cancellationToken);
                _logger.LogDebug($"Secret {LoggerHelper.SanitizeValue(secretName)} created in key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                return keyVaultSecret.Properties.Version;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                throw new BadRequestException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to create specified secret.", ex);
            }
        }

        /// <summary>
        /// Create a secret in azure key vault.
        /// </summary>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="secretValue">The value of the secret.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The version of the secret if secret is created successfully.</returns>
        public async Task<string> CreateSecretAsync(string secretName, string secretValue, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (keyVaultUrl != null)
                {
                    SetSecretClient(keyVaultUrl);
                }
                else
                {
                    SetSecretClient();
                }

                KeyVaultSecret keyVaultSecret = await KeyVaultSecretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
                _logger.LogDebug($"Secret {LoggerHelper.SanitizeValue(secretName)} created in key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");
                return keyVaultSecret.Properties.Version;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                throw new BadRequestException($"Secret Name:{secretName}", $"{ex.Status} - Service request failed, unable to create specified secret.", ex);
            }
        }

        /// <summary>
        /// Create a secret in azure key vault.
        /// </summary>
        /// <param name="azureKeyVaultSecret">The Secret object containing information about the secret and its properties.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The version of the secret if secret is created successfully.</returns>
        public string CreateSecret(AzureKeyVaultSecret azureKeyVaultSecret, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (keyVaultUrl != null)
                {
                    SetSecretClient(keyVaultUrl);
                }
                else
                {
                    SetSecretClient();
                }

                KeyVaultSecret keyVaultSecret = KeyVaultSecretClient.SetSecret(azureKeyVaultSecret, cancellationToken);
                _logger.LogDebug($"Secret {LoggerHelper.SanitizeValue(keyVaultSecret.Name)} created in key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");

                return keyVaultSecret.Properties.Version;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                throw new BadRequestException(nameof(azureKeyVaultSecret.Name), $"{ex.Status} - Service request failed, unable to create specified secret.", ex);
            }
        }

        /// <summary>
        /// Create a secret in azure key vault.
        /// </summary>
        /// <param name="azureKeyVaultSecret">The Secret object containing information about the secret and its properties.</param>
        /// <param name="keyVaultUrl">Key Vault URL.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>The version of the secret if secret is created successfully.</returns>
        public async Task<string> CreateSecretAsync(AzureKeyVaultSecret azureKeyVaultSecret, Uri keyVaultUrl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (keyVaultUrl != null)
                {
                    SetSecretClient(keyVaultUrl);
                }
                else
                {
                    SetSecretClient();
                }

                KeyVaultSecret keyVaultSecret = await KeyVaultSecretClient.SetSecretAsync(azureKeyVaultSecret, cancellationToken);
                _logger.LogDebug($"Secret {LoggerHelper.SanitizeValue(keyVaultSecret.Name)} created in key vault {LoggerHelper.SanitizeValue(keyVaultUrl) ?? LoggerHelper.SanitizeValue(_azureKeyVaultSettings.Url)}.");

                return keyVaultSecret.Properties.Version;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"{ex.Status} - {ex.Message}");
                throw new BadRequestException(nameof(azureKeyVaultSecret.Name), $"{ex.Status} - Service request failed, unable to create specified secret.", ex);
            }
        }

        private SecretClientOptions GetSecretClientOptions()
        {
            SecretClientOptions secretClientOptions = new SecretClientOptions();
            secretClientOptions.Retry.Mode = RetryMode.Exponential;
            secretClientOptions.Retry.MaxRetries = _azureKeyVaultSettings.MaxRetries;
            secretClientOptions.Retry.Delay = TimeSpan.FromSeconds(_azureKeyVaultSettings.RetryDelay);
            secretClientOptions.Retry.MaxDelay = TimeSpan.FromSeconds(_azureKeyVaultSettings.MaxDelay);
            return secretClientOptions;
        }

        private void SetSecretClient()
        {
            if (_azureKeyVaultSettings.AuthenticationType == AzureKeyVaultAuthenticationType.MSI)
            {
                if (_azureKeyVaultSettings.Url != null)
                {
                    if (CurrentCredential == null)
                    {
                        CurrentCredential = ExosCredentials.GetDefaultCredential(_configuration);
                    }

                    KeyVaultSecretClient = new SecretClient(_azureKeyVaultSettings.Url, CurrentCredential, GetSecretClientOptions());
                }
                else
                {
                    throw new ArgumentException("Provide a Key Vault Url in appsettings for MSI authentication.");
                }
            }
            else
            {
                KeyVaultSecretClient = new SecretClient(_azureKeyVaultSettings.Url, new ClientSecretCredential(_azureKeyVaultSettings.TenantId, _azureKeyVaultSettings.ClientId, _azureKeyVaultSettings.ClientSecret), GetSecretClientOptions());
            }
        }

        private void SetSecretClient(Uri keyVaultUrl)
        {
            if (CurrentCredential == null)
            {
                CurrentCredential = ExosCredentials.GetDefaultCredential(_configuration);
            }

            // Set the keyvault using MSI
            KeyVaultSecretClient = new SecretClient(keyVaultUrl, CurrentCredential, GetSecretClientOptions());
        }
    }
}
