namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Collections.Generic;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.KeyVault;
    using Exos.Platform.AspNetCore.Middleware;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Implements IEncryptionKeyFinder, to find encryption keys in key vault file.
    /// </summary>
    public class KeyVaultEncryptionKeyFinder : IEncryptionKeyFinder
    {
        private readonly ILogger<KeyVaultEncryptionKeyFinder> _logger;
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly EncryptionKeyVaultSecretClient _azureKeyVaultSecretClient;
        private readonly List<EncryptionKeyMapping> _encryptionKeyMappings;
        private readonly IMemoryCache _memoryCache;
        private readonly EncryptionKeyVaultSettings _azureKeyVaultSettings;
        private readonly ICommonKeyEncryptionFinder _commonKeyEncryptionFinder;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultEncryptionKeyFinder"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger{KeyVaultEncryptionKeyFinder}"/>.</param>
        /// <param name="userHttpContextAccessorService"><see cref="IUserHttpContextAccessorService"/>.</param>
        /// <param name="azureKeyVaultSecretClient"><see cref="AzureKeyVaultSecretClient"/>.</param>
        /// <param name="encryptionKeyMappingConfiguration"><see cref="IOptions{EncryptionKeyMappingConfiguration}"/>.</param>
        /// <param name="memoryCache"><see cref="IMemoryCache"/>.</param>
        /// <param name="azureKeyVaultSettings"><see cref="IOptions{AzureKeyVaultSettings}"/>.</param>
        /// <param name="commonKeyEncryptionFinder">Key finder.</param>
        public KeyVaultEncryptionKeyFinder(
            ILogger<KeyVaultEncryptionKeyFinder> logger,
            IUserHttpContextAccessorService userHttpContextAccessorService,
            EncryptionKeyVaultSecretClient azureKeyVaultSecretClient,
            IOptions<EncryptionKeyMappingConfiguration> encryptionKeyMappingConfiguration,
            IMemoryCache memoryCache,
            IOptions<EncryptionKeyVaultSettings> azureKeyVaultSettings,
            ICommonKeyEncryptionFinder commonKeyEncryptionFinder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userHttpContextAccessorService = userHttpContextAccessorService ?? throw new ArgumentNullException(nameof(userHttpContextAccessorService));
            _azureKeyVaultSecretClient = azureKeyVaultSecretClient ?? throw new ArgumentNullException(nameof(azureKeyVaultSecretClient));
            // Load the Key Mapping
            _encryptionKeyMappings = encryptionKeyMappingConfiguration != null && encryptionKeyMappingConfiguration.Value.EncryptionKeyMappings != null ?
                encryptionKeyMappingConfiguration.Value.EncryptionKeyMappings : throw new ArgumentNullException(nameof(encryptionKeyMappingConfiguration));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _azureKeyVaultSettings = azureKeyVaultSettings != null && azureKeyVaultSettings.Value != null ?
                azureKeyVaultSettings.Value : throw new ArgumentNullException(nameof(azureKeyVaultSettings));

            _commonKeyEncryptionFinder = commonKeyEncryptionFinder ?? throw new ArgumentNullException(nameof(commonKeyEncryptionFinder));
        }

        /// <inheritdoc/>
        public string FindKeyValue(string keyName, string keyVersion = null)
        {
            string keyValue;
            if (string.IsNullOrEmpty(keyVersion))
            {
                // Return current version
                EncryptionKey encryptionKey = FindEncryptionKey(keyName);
                keyValue = encryptionKey.KeyValue;
            }
            else
            {
                // Find Encryption key by Version
                EncryptionKey encryptionKey = FindEncryptionKeyByVersion(keyName, keyVersion);
                keyValue = encryptionKey.KeyValue;
            }

            return keyValue;
        }

        /// <inheritdoc/>
        public string FindKeyNameBase(string keyName, string keyVersion = null)
        {
            string keyNameBase;
            if (string.IsNullOrEmpty(keyVersion))
            {
                // Return current version
                EncryptionKey encryptionKey = FindEncryptionKey(keyName);
                keyNameBase = encryptionKey.KeyNameBase;
            }
            else
            {
                // Find Encryption key by Version
                EncryptionKey encryptionKey = FindEncryptionKeyByVersion(keyName, keyVersion);
                keyNameBase = encryptionKey.KeyNameBase;
            }

            return keyNameBase;
        }

        /// <inheritdoc/>
        public EncryptionKey GetCommonEncryptionKey()
        {
            return _commonKeyEncryptionFinder.GetCommonEncryptionKey();
        }

        /// <inheritdoc/>
        public EncryptionKey GetCurrentEncryptionKey(string keyIdentifier = null)
        {
            if (string.IsNullOrEmpty(keyIdentifier))
            {
                // Get the subdomain of the request like boadev-s3.exostechnology.com
                keyIdentifier = _userHttpContextAccessorService.GetClientKeyIdentifier();
            }

            // Find the key mapping for the keyIdentifier
            EncryptionKeyMapping encryptionKeyMapping = _encryptionKeyMappings.Find(x => x.KeyIdentifier == keyIdentifier);

            if (encryptionKeyMapping != null)
            {
                EncryptionKey encryptionKey = FindEncryptionKey(encryptionKeyMapping.KeyName, keyIdentifier);
                return encryptionKey;
            }
            else
            {
                throw new NotFoundException("x-client-tag", $"Invalid value in Encryption header/identifier or not present in the request: {LoggerHelper.SanitizeValue(keyIdentifier)}");
            }
        }

        /// <inheritdoc/>
        public EncryptionKey GetEncryptionKey(string keyName, string keyVersion = null)
        {
            if (string.IsNullOrEmpty(keyVersion))
            {
                // Return current version
                EncryptionKey encryptionKey = FindEncryptionKey(keyName);
                return encryptionKey;
            }
            else
            {
                // Find Encryption key by Version
                EncryptionKey encryptionKey = FindEncryptionKeyByVersion(keyName, keyVersion);
                return encryptionKey;
            }
        }

        private static MemoryCacheEntryOptions GetMemoryCacheOptions(double reloadInterval)
        {
            var cacheExpirationOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                Priority = CacheItemPriority.Normal,
                SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
            };
            return cacheExpirationOptions;
        }

        private EncryptionKey FindEncryptionKey(string keyName, string keyIdentifier = null)
        {
            if (!_memoryCache.TryGetValue(keyName, out EncryptionKey encryptionKey))
            {
                EncryptionKeyMapping encryptionKeyMapping = _encryptionKeyMappings.Find(x => x.KeyName == keyName);
                string keyValue = _azureKeyVaultSecretClient.GetSecretValue(keyName, out string version, encryptionKeyMapping?.KeyVaultUrl);
                encryptionKey = new EncryptionKey
                {
                    KeyIdentifier = keyIdentifier ?? encryptionKeyMapping?.KeyIdentifier,
                    KeyName = keyName,
                    KeyVersion = version,
                    KeyValue = keyValue,
                    KeyValueBytes = EncryptionKey.GetKeyBytes(keyValue),
                    KeyNameBase = encryptionKeyMapping?.KeyNameBase
                };
                double reloadInterval = _azureKeyVaultSettings.ReloadInterval;
                _memoryCache.Set(keyName, encryptionKey, GetMemoryCacheOptions(reloadInterval));
            }

            return encryptionKey;
        }

        private EncryptionKey FindEncryptionKeyByVersion(string keyName, string keyVersion)
        {
            string cacheKey = $"{keyName}{IDatabaseEncryption.EncryptedHeaderDelimiter}{keyVersion}";
            if (!_memoryCache.TryGetValue(cacheKey, out EncryptionKey encryptionKey))
            {
                // Find the default key version in Key Vault
                EncryptionKeyMapping encryptionKeyMapping = _encryptionKeyMappings.Find(x => x.KeyName == keyName);
                string keyValue = _azureKeyVaultSecretClient.GetSecretValue(keyName, keyVersion, encryptionKeyMapping?.KeyVaultUrl);
                encryptionKey = new EncryptionKey
                {
                    KeyIdentifier = encryptionKeyMapping?.KeyIdentifier,
                    KeyName = keyName,
                    KeyVersion = keyVersion,
                    KeyValue = keyValue,
                    KeyValueBytes = EncryptionKey.GetKeyBytes(keyValue),
                    KeyNameBase = encryptionKeyMapping?.KeyNameBase
                };
                double reloadInterval = _azureKeyVaultSettings.ReloadInterval > 0 ? _azureKeyVaultSettings.ReloadInterval : 60;
                _memoryCache.Set(cacheKey, encryptionKey, GetMemoryCacheOptions(reloadInterval));
            }

            return encryptionKey;
        }
    }
}