namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Middleware;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Implements IEncryptionKeyFinder, to find encryption keys in appsettings.json file.
    /// </summary>
    public class AppSettingsEncryptionKeyFinder : IEncryptionKeyFinder
    {
        private readonly ILogger<AppSettingsEncryptionKeyFinder> _logger;
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly ICommonKeyEncryptionFinder _commonKeyEncryptionFinder;
        private readonly List<EncryptionKey> _encryptionKeys;
        private readonly List<EncryptionKeyMapping> _encryptionKeyMappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsEncryptionKeyFinder"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="userHttpContextAccessorService"><see cref="IUserHttpContextAccessorService"/>.</param>
        /// <param name="encryptionKeyMappingConfiguration"><see cref="EncryptionKeyMappingConfiguration"/>.</param>
        /// <param name="encryptionKeyConfiguration"><see cref="EncryptionKeyConfiguration"/>.</param>
        /// <param name="commonCommonKeyEncryptionFinder">vault finder.</param>
        public AppSettingsEncryptionKeyFinder(
            ILogger<AppSettingsEncryptionKeyFinder> logger,
            IUserHttpContextAccessorService userHttpContextAccessorService,
            IOptions<EncryptionKeyMappingConfiguration> encryptionKeyMappingConfiguration,
            IOptions<EncryptionKeyConfiguration> encryptionKeyConfiguration,
            ICommonKeyEncryptionFinder commonCommonKeyEncryptionFinder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userHttpContextAccessorService = userHttpContextAccessorService ?? throw new ArgumentNullException(nameof(userHttpContextAccessorService));
            // Load the Key Mapping
            _encryptionKeyMappings = encryptionKeyMappingConfiguration != null ? encryptionKeyMappingConfiguration.Value.EncryptionKeyMappings : throw new ArgumentNullException(nameof(encryptionKeyMappingConfiguration));
            // Load the encryption Keys
            _encryptionKeys = encryptionKeyConfiguration != null ? encryptionKeyConfiguration.Value.EncryptionKeys : throw new ArgumentNullException(nameof(encryptionKeyConfiguration));

            _commonKeyEncryptionFinder = commonCommonKeyEncryptionFinder;
        }

        /// <inheritdoc/>
        public string FindKeyValue(string keyName, string keyVersion = null)
        {
            if (_encryptionKeys != null && _encryptionKeys.Any())
            {
                string keyValue;
                if (string.IsNullOrEmpty(keyVersion))
                {
                    keyValue = _encryptionKeys.Find(x => x.KeyName == keyName)?.KeyValue;
                }
                else
                {
                    keyValue = _encryptionKeys.Find(x => x.KeyName == keyName && x.KeyVersion == keyVersion)?.KeyValue;
                }

                return keyValue;
            }

            return null;
        }

        /// <inheritdoc/>
        public string FindKeyNameBase(string keyName, string keyVersion = null)
        {
            if (_encryptionKeys != null && _encryptionKeys.Any())
            {
                string keyNameBase;
                if (string.IsNullOrEmpty(keyVersion))
                {
                    keyNameBase = _encryptionKeys.Find(x => x.KeyName == keyName)?.KeyNameBase;
                }
                else
                {
                    keyNameBase = _encryptionKeys.Find(x => x.KeyName == keyName && x.KeyVersion == keyVersion)?.KeyNameBase;
                }

                return keyNameBase;
            }

            return null;
        }

        /// <inheritdoc/>
        public EncryptionKey GetCommonEncryptionKey()
        {
            var encryptionKey = _encryptionKeys.Find(x => x.KeyName == _commonKeyEncryptionFinder.CommonEncryptionKeyName);
            if (encryptionKey == null)
            {
                return null;
            }

            encryptionKey.KeyIdentifier = _commonKeyEncryptionFinder.CommonEncryptionKeyName;
            encryptionKey.KeyNameBase = _commonKeyEncryptionFinder.CommonEncryptionKeyBaseName;
            encryptionKey.KeyValueBytes = EncryptionKey.GetKeyBytes(encryptionKey.KeyValue);
            return encryptionKey;
        }

        /// <inheritdoc/>
        public EncryptionKey GetCurrentEncryptionKey(string keyIdentifier = null)
        {
            if (string.IsNullOrEmpty(keyIdentifier))
            {
                // Get the subdomain of the request like boadev-s3.exostechnology.com
                keyIdentifier = _userHttpContextAccessorService.GetClientKeyIdentifier();
            }

            if (_encryptionKeyMappings != null && _encryptionKeyMappings.Any())
            {
                // Find the key mapping for the keyIdentifier
                EncryptionKeyMapping encryptionKeyMapping = _encryptionKeyMappings.Find(x => x.KeyIdentifier == keyIdentifier);

                if (encryptionKeyMapping != null)
                {
                    EncryptionKey encryptionKey = _encryptionKeys.Find(x => x.KeyName == encryptionKeyMapping.KeyName);
                    if (encryptionKey != null)
                    {
                        encryptionKey.KeyIdentifier = keyIdentifier;
                        encryptionKey.KeyValueBytes = EncryptionKey.GetKeyBytes(encryptionKey.KeyValue);
                        encryptionKey.KeyNameBase = encryptionKeyMapping.KeyNameBase;
                    }

                    return encryptionKey;
                }
                else
                {
                    throw new NotFoundException("x-client-tag", $"Invalid value in Encryption header/identifier or not present in the request: {LoggerHelper.SanitizeValue(keyIdentifier)}");
                }
            }

            return new EncryptionKey();
        }

        /// <inheritdoc/>
        public EncryptionKey GetEncryptionKey(string keyName, string keyVersion = null)
        {
            if (_encryptionKeys != null && _encryptionKeys.Any())
            {
                if (string.IsNullOrEmpty(keyVersion))
                {
                    return _encryptionKeys.Find(x => x.KeyName == keyName);
                }
                else
                {
                   return _encryptionKeys.Find(x => x.KeyName == keyName && x.KeyVersion == keyVersion);
                }
            }

            return null;
        }
    }
}