namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// config file based common key finder, used primarily in unit tests.
    /// </summary>
    public class AppSettingsCommonKeyEncryptionFinder : ICommonKeyEncryptionFinder
    {
        private readonly ILogger<AppSettingsCommonKeyEncryptionFinder> _logger;
        private readonly List<EncryptionKey> _encryptionKeys;
        private readonly List<EncryptionKeyMapping> _encryptionKeyMappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsCommonKeyEncryptionFinder"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="encryptionKeyMappingConfiguration"><see cref="EncryptionKeyMappingConfiguration"/>.</param>
        /// <param name="encryptionKeyConfiguration"><see cref="EncryptionKeyConfiguration"/>.</param>
        public AppSettingsCommonKeyEncryptionFinder(
            ILogger<AppSettingsCommonKeyEncryptionFinder> logger,
            IOptions<EncryptionKeyMappingConfiguration> encryptionKeyMappingConfiguration,
            IOptions<EncryptionKeyConfiguration> encryptionKeyConfiguration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Load the Key Mapping
            _encryptionKeyMappings = encryptionKeyMappingConfiguration != null ? encryptionKeyMappingConfiguration.Value.EncryptionKeyMappings : throw new ArgumentNullException(nameof(encryptionKeyMappingConfiguration));
            // Load the encryption Keys
            _encryptionKeys = encryptionKeyConfiguration != null ? encryptionKeyConfiguration.Value.EncryptionKeys : throw new ArgumentNullException(nameof(encryptionKeyConfiguration));
        }

        /// <inheritdoc/>
        public string CommonEncryptionKeyName => "key--exos-svclnk";

        /// <inheritdoc/>
        public string CommonEncryptionKeyBaseName => "key--exos-svclnk";

        /// <inheritdoc/>
        public string CommonHashingSaltName => "key--exos-dbhashing-salt";

        /// <inheritdoc/>
        public string CommonHashingSaltBaseName => "key--exos-dbhashing-salt";

        /// <inheritdoc/>
        public EncryptionKey GetCommonHashingSalt()
        {
            var encryptionKey = _encryptionKeys.Find(x => x.KeyName == CommonHashingSaltName);
            if (encryptionKey == null)
            {
                return null;
            }

            encryptionKey.KeyIdentifier = CommonHashingSaltName;
            encryptionKey.KeyNameBase = CommonHashingSaltBaseName;
            encryptionKey.KeyValueBytes = EncryptionKey.GetKeyBytes(encryptionKey.KeyValue);
            return encryptionKey;
        }

        /// <inheritdoc/>
        public EncryptionKey GetCommonEncryptionKey()
        {
            var encryptionKey = _encryptionKeys.Find(x => x.KeyName == CommonEncryptionKeyName);
            if (encryptionKey == null)
            {
                return null;
            }

            encryptionKey.KeyIdentifier = CommonEncryptionKeyName;
            encryptionKey.KeyNameBase = CommonEncryptionKeyBaseName;
            encryptionKey.KeyValueBytes = EncryptionKey.GetKeyBytes(encryptionKey.KeyValue);
            return encryptionKey;
        }
    }
}