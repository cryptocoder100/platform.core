namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="DatabaseHashing" />.
    /// </summary>
    public class DatabaseHashing : IDatabaseHashing
    {
        private const int ITERATIONCOUNT = 10000;
        private const int HASHLENGTHBYTES = 32;
        private readonly ILogger<DatabaseHashing> _logger;
        private readonly ICommonKeyEncryptionFinder _commonKeyEncryptionFinder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHashing"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{DatabaseHashing}"/>.</param>
        /// <param name="commonKeyEncryptionFinder"><see cref="IEncryptionKeyFinder"/>.</param>
        public DatabaseHashing(ILogger<DatabaseHashing> logger, ICommonKeyEncryptionFinder commonKeyEncryptionFinder)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commonKeyEncryptionFinder = commonKeyEncryptionFinder ?? throw new ArgumentNullException(nameof(commonKeyEncryptionFinder));
        }

        /// <inheritdoc/>
        public string HashStringToHex(string valueToHash)
        {
            if (string.IsNullOrEmpty(valueToHash))
            {
                throw new ArgumentNullException(nameof(valueToHash));
            }

            byte[] salt = GetSaltValue();
            byte[] hashValue = KeyDerivation.Pbkdf2(valueToHash, salt, KeyDerivationPrf.HMACSHA512, ITERATIONCOUNT, HASHLENGTHBYTES);
            // Convert to hex
            string hashString = BitConverter.ToString(hashValue).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
            return hashString;
        }

        /// <inheritdoc/>
        public byte[] HashStringToByte(string valueToHash)
        {
            if (string.IsNullOrEmpty(valueToHash))
            {
                throw new ArgumentNullException(nameof(valueToHash));
            }

            byte[] salt = GetSaltValue();
            byte[] hashValue = KeyDerivation.Pbkdf2(valueToHash, salt, KeyDerivationPrf.HMACSHA512, ITERATIONCOUNT, HASHLENGTHBYTES);
            return hashValue;
        }

        /// <inheritdoc/>
        public string HashStringToBase64(string valueToHash)
        {
            if (string.IsNullOrEmpty(valueToHash))
            {
                throw new ArgumentNullException(nameof(valueToHash));
            }

            byte[] salt = GetSaltValue();
            byte[] hashValue = KeyDerivation.Pbkdf2(valueToHash, salt, KeyDerivationPrf.HMACSHA512, ITERATIONCOUNT, HASHLENGTHBYTES);
            // Convert to base64
            string hashBase64String = Convert.ToBase64String(hashValue);
            return hashBase64String;
        }

        private byte[] GetSaltValue()
        {
            EncryptionKey dbhashingSecret = _commonKeyEncryptionFinder.GetCommonHashingSalt();
            byte[] salt = dbhashingSecret.KeyValueBytes;
            return salt;
        }
    }
}
