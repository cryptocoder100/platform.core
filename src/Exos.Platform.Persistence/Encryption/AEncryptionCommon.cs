namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Middleware;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class that contains common logic shared between the AES-GCM and AES-CBC Libraries.
    /// </summary>
    public abstract class AEncryptionCommon
    {
        /// <summary>
        /// The key finder that comes in from the implementing class.
        /// </summary>
        private readonly IEncryptionKeyFinder _encryptionKeyFinder;

        private readonly ILogger<AEncryptionCommon> _logger;

        /// <summary>
        /// the current encryption key used in the routine.
        /// </summary>
        private EncryptionKey _currentEncryptionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="AEncryptionCommon"/> class.
        /// </summary>
        /// <param name="encryptionKeyFinder">the encryption finder.</param>
        /// <param name="logger">the logger.</param>
        protected AEncryptionCommon(IEncryptionKeyFinder encryptionKeyFinder, ILogger<AEncryptionCommon> logger)
        {
            _encryptionKeyFinder = encryptionKeyFinder ?? throw new ArgumentNullException(nameof(encryptionKeyFinder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the request
        /// validates the key for decryption if is set to false
        /// the request can decrypt any record.
        /// </summary>
        public bool ValidateKeyForDecryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the current encryption key.
        /// </summary>
        public EncryptionKey CurrentEncryptionKey
        {
            get { return _currentEncryptionKey ??= GetCurrentEncryptionKey(); }
            set => _currentEncryptionKey = value;
        }

        /// <summary>
        /// Set the encryption key to use.
        /// </summary>
        /// <param name="keyIdentifier">The keyIdentifier<see cref="string"/> to find the key name in the EncryptionKeyMappingConfiguration.</param>
        /// <returns>Client Encryption Key.</returns>
        public EncryptionKey SetEncryptionKey(string keyIdentifier)
        {
            EncryptionKey encryptionKey = _encryptionKeyFinder.GetCurrentEncryptionKey(keyIdentifier);
            CurrentEncryptionKey = encryptionKey;
            return encryptionKey;
        }

        /// <summary>
        ///  Set the encryption key to use.
        /// </summary>
        /// <param name="encryptionKey">The encryptionKey<see cref="EncryptionKey"/>.</param>
        /// <returns>The <see cref="EncryptionKey"/>.</returns>
        public EncryptionKey SetEncryptionKey(EncryptionKey encryptionKey)
        {
            _ = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
            LoadEncryptionKeyWithValue(encryptionKey);
            CurrentEncryptionKey = encryptionKey;
            return encryptionKey;
        }

        /// <summary>
        /// Runs all logic to determine if the key is valid to use and passes security.
        /// </summary>
        /// <param name="encryptionKey">The key to check.</param>
        /// <returns>Key to use.</returns>
        /// <exception cref="UnauthorizedException">Thrown if uri isn't authorized to see.</exception>
        protected EncryptionKey ValidateDecryptionKey(EncryptionKey encryptionKey)
        {
            _ = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));

            if (ValidateKeyForDecryption)
            {
                // If is encrypted with the current key version don't get the value from Key Store.
                if (CurrentEncryptionKey.Equals(encryptionKey))
                {
                    encryptionKey.KeyValue = _currentEncryptionKey.KeyValue;
                    encryptionKey.KeyValueBytes = _currentEncryptionKey.KeyValueBytes;
                    encryptionKey.KeyNameBase = _currentEncryptionKey.KeyNameBase;
                }
                else if (string.Equals(CurrentEncryptionKey.KeyName, encryptionKey.KeyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(encryptionKey.KeyVersion))
                    {
                        // Get Default Version
                        LoadEncryptionKeyWithValue(encryptionKey);
                    }
                    else
                    {
                        // Encrypted with a different version getting the key value from the Key Store.
                        LoadEncryptionKeyWithPreviousValue(encryptionKey);
                    }
                }
                else if (encryptionKey?.KeyName != null && CurrentEncryptionKey?.KeyNameBase != null
                    && encryptionKey.KeyName.StartsWith(CurrentEncryptionKey.KeyNameBase, StringComparison.OrdinalIgnoreCase))
                {
                    // Validate if we need to decrypt with a previous secret for example.
                    // value is encrypted with key--exos-boa but the current key in the system is key--exos-boa--v2
                    LoadEncryptionKeyWithValue(encryptionKey);
                    return encryptionKey;
                }
                else
                {
                    string errorMessage;
                    if (string.IsNullOrEmpty(CurrentEncryptionKey.KeyName))
                    {
                        // Trying to read encrypted data and the key for the domain is not configured.
                        errorMessage = $"Data is encrypted with Key:{LoggerHelper.SanitizeValue(encryptionKey.KeyName)} and user is not authorized read encrypted data";
                    }
                    else
                    {
                        // Our current key doesn't match the one used to encrypt this data.
                        // If the actual encryption key is flagged as a fail-over (or 'common') key, then ago ahead and decrypt.
                        EncryptionKey commonKey = _encryptionKeyFinder.GetCommonEncryptionKey();
                        if (encryptionKey.KeyName.StartsWith(commonKey.KeyNameBase, StringComparison.OrdinalIgnoreCase))
                        {
                            // the key passed is a common key as best we can determine.  Resolve it.
                            if (encryptionKey.KeyVersion.Equals(commonKey.KeyVersion, StringComparison.OrdinalIgnoreCase))
                            {
                                // This is required for blob decryption to validate HMAC
                                if (!string.IsNullOrEmpty(encryptionKey.HmacBase64))
                                {
                                    commonKey.HmacBase64 = encryptionKey.HmacBase64;
                                }

                                return commonKey;
                            }

                            // Encrypted with a different common key getting the key value from the Key Store.
                            LoadEncryptionKeyWithValue(encryptionKey);
                            return encryptionKey;
                        }

                        // Encrypted with a different key than the user has access to use throwing not authorized exception
                        errorMessage = $"Data is encrypted with Key:{LoggerHelper.SanitizeValue(encryptionKey.KeyName)} and user is authorized to use the Key:{LoggerHelper.SanitizeValue(CurrentEncryptionKey.KeyName)}";
                    }

                    _logger.LogError(errorMessage);
                    // Throw a generic message
                    throw new UnauthorizedException("User not authorized to read.");
                }
            }
            else
            {
                // If is encrypted with the current key version don't get the value from Key Store.
                if (CurrentEncryptionKey.Equals(encryptionKey))
                {
                    encryptionKey.KeyValue = _currentEncryptionKey.KeyValue;
                    encryptionKey.KeyValueBytes = _currentEncryptionKey.KeyValueBytes;
                }
                else
                {
                    if (string.IsNullOrEmpty(encryptionKey.KeyVersion))
                    {
                        // Get Default Version
                        LoadEncryptionKeyWithValue(encryptionKey);
                    }
                    else
                    {
                        // Encrypted with a different key/version getting the key value from the Key Store.
                        LoadEncryptionKeyWithPreviousValue(encryptionKey);
                    }
                }
            }

            return encryptionKey;
        }

        private void LoadEncryptionKeyWithValue(EncryptionKey encryptionKey)
        {
            string keyValue = _encryptionKeyFinder.FindKeyValue(encryptionKey.KeyName);
            encryptionKey.KeyValue = keyValue;
            encryptionKey.KeyValueBytes = EncryptionKey.GetKeyBytes(keyValue);
            if (string.IsNullOrEmpty(encryptionKey.KeyNameBase))
            {
                encryptionKey.KeyNameBase = _encryptionKeyFinder.FindKeyNameBase(encryptionKey.KeyName);
            }
        }

        private void LoadEncryptionKeyWithPreviousValue(EncryptionKey encryptionKey)
        {
            string keyValue = _encryptionKeyFinder.FindKeyValue(encryptionKey.KeyName, encryptionKey.KeyVersion);
            encryptionKey.KeyValue = keyValue;
            encryptionKey.KeyValueBytes = EncryptionKey.GetKeyBytes(keyValue);
            if (string.IsNullOrEmpty(encryptionKey.KeyNameBase))
            {
                encryptionKey.KeyNameBase = _encryptionKeyFinder.FindKeyNameBase(encryptionKey.KeyName, encryptionKey.KeyVersion);
            }
        }

        private EncryptionKey GetCurrentEncryptionKey()
        {
            EncryptionKey currentEncryptionKey = _encryptionKeyFinder.GetCurrentEncryptionKey();
            return currentEncryptionKey;
        }
    }
}