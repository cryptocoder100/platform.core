using System;
using Exos.Platform.AspNetCore.Authentication;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.Persistence.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Exos.Platform.Persistence.Encryption
{
    /// <summary>
    /// Repository designed to retrieve the standard keys in the Exos System.
    /// </summary>
    public class KeyVaultCommonKeyEncryptionFinder : ICommonKeyEncryptionFinder
    {
        private readonly EncryptionKeyVaultSettings _azureKeyVaultSettings;
        private readonly EncryptionKeyVaultSecretClient _azureKeyVaultSecretClient;
        private readonly IConfiguration _appConfigResults;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultCommonKeyEncryptionFinder"/> class.
        /// </summary>
        /// <param name="keyVaultSecretClient">The vault client to use.</param>
        /// <param name="azureKeyVaultSettings">Vault settings.</param>
        /// <param name="configuration">configuration settings.</param>
        /// <param name="memoryCache">memoryCache settings.</param>
        public KeyVaultCommonKeyEncryptionFinder(
            EncryptionKeyVaultSecretClient keyVaultSecretClient,
            IOptions<EncryptionKeyVaultSettings> azureKeyVaultSettings,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _azureKeyVaultSecretClient = keyVaultSecretClient ?? throw new ArgumentNullException(nameof(keyVaultSecretClient));
            _azureKeyVaultSettings = azureKeyVaultSettings != null && azureKeyVaultSettings.Value != null ? azureKeyVaultSettings.Value : throw new ArgumentNullException(nameof(azureKeyVaultSettings));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            string cachedConfigKey = "KeyVaultCommonKeyEncryptionFinder_cached_config";

            if (!_memoryCache.TryGetValue(cachedConfigKey, out _appConfigResults))
            {
                var url = ConfigurationHelper.GetAppConfigurationUrl(configuration);

                if (string.IsNullOrEmpty(url))
                {
                    throw new ServiceNotProperlyConfiguredException();
                }

                var credentials = ExosCredentials.GetDefaultCredential(configuration);

                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(new Uri(url), credentials)
                        .Select("ExosMacro:Encryption:*")
                        .ConfigureKeyVault(kv => { kv.SetCredential(credentials); });
                });

                _appConfigResults = configBuilder.Build();
                _memoryCache.Set(cachedConfigKey, _appConfigResults, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_azureKeyVaultSettings.ReloadInterval)
                });
            }
        }

        /// <inheritdoc/>
        public string CommonHashingSaltName
        {
            get
            {
                var value = _appConfigResults.GetValue<string>($"ExosMacro:Encryption:SvclnkDbHashSaltKey");
                return value;
            }
        }

        /// <inheritdoc/>
        public string CommonHashingSaltBaseName => "key--exos-dbhashing-salt";

        /// <inheritdoc/>
        public string CommonEncryptionKeyName
        {
            get
            {
                var value = _appConfigResults.GetValue<string>($"ExosMacro:Encryption:SvclnkKey");
                return value;
            }
        }

        /// <inheritdoc/>
        public string CommonEncryptionKeyBaseName => "key--exos-svclnk";

        /// <inheritdoc/>
        public EncryptionKey GetCommonHashingSalt()
        {
            if (!_memoryCache.TryGetValue(CommonHashingSaltName, out EncryptionKey encryptionKey))
            {
                encryptionKey = BuildKey(CommonHashingSaltName, "exos-db-hashing-salt", CommonHashingSaltBaseName);
            }

            return encryptionKey;
        }

        /// <inheritdoc/>
        public EncryptionKey GetCommonEncryptionKey()
        {
            if (!_memoryCache.TryGetValue(CommonEncryptionKeyName, out EncryptionKey encryptionKey))
            {
                encryptionKey = BuildKey(CommonEncryptionKeyName, "key--exos-svclnk", CommonEncryptionKeyBaseName);
            }

            return encryptionKey;
        }

        private EncryptionKey BuildKey(string secretName, string identifier, string baseName)
        {
            string encryptionKey = _azureKeyVaultSecretClient.GetSecretValue(secretName, out string keyVersion, _azureKeyVaultSettings.Url);

            var newKey = new EncryptionKey
            {
                KeyName = secretName,
                KeyVersion = keyVersion,
                KeyNameBase = baseName,
                KeyIdentifier = identifier,
                KeyValue = encryptionKey,
                KeyValueBytes = EncryptionKey.GetKeyBytes(encryptionKey)
            };

            _memoryCache.Set(secretName, newKey, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_azureKeyVaultSettings.ReloadInterval)
            });

            return newKey;
        }
    }
}