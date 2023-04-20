namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Text;
    using Exos.Platform.Persistence.Entities;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implementation of IDatabase Encryption using the
    /// Advanced Encryption Standard (AES)  with the Galois/Counter Mode (GCM) mode of operation.
    /// Use the ClientKeyIdentifier from the user context to find the key for encryption / decryption.
    /// </summary>
    public class AesGcmDatabaseEncryption : AEncryptionCommon, IDatabaseEncryption
    {
        private const int _nonceBytes = 12;
        private const int _tagBytes = 16;
        private readonly ILogger<AesGcmDatabaseEncryption> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesGcmDatabaseEncryption"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="encryptionKeyFinder"><see cref="IEncryptionKeyFinder"/>.</param>
        public AesGcmDatabaseEncryption(ILogger<AesGcmDatabaseEncryption> logger, IEncryptionKeyFinder encryptionKeyFinder) : base(encryptionKeyFinder, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public string Encrypt(string stringToEncrypt, EncryptionKey encryptionKey = null, bool concatenateKey = true)
        {
            encryptionKey ??= CurrentEncryptionKey;
            if (encryptionKey.IsKeyReadyToUse)
            {
                byte[] encryptedBytes = EncryptBytes(Encoding.UTF8.GetBytes(stringToEncrypt), encryptionKey);
                string encryptedValue = Convert.ToBase64String(encryptedBytes);
                if (concatenateKey)
                {
                    encryptedValue = encryptionKey.AssembleFinalFieldValue(encryptedValue);
                }

                return encryptedValue;
            }
            else
            {
                return stringToEncrypt;
            }
        }

        /// <inheritdoc/>
        public string Decrypt(string stringToDecrypt)
        {
            if (string.IsNullOrEmpty(stringToDecrypt))
            {
                return stringToDecrypt;
            }

            EncryptedFieldHeaderParser headerParser = new EncryptedFieldHeaderParser(stringToDecrypt);

            if (headerParser.IsEncrypted)
            {
                EncryptionKey encryptionKey = headerParser.EncryptionKey;
                encryptionKey = ValidateDecryptionKey(encryptionKey);
                if (encryptionKey.IsKeyReadyToUse)
                {
                    byte[] decryptBytes = DecryptBytes(Convert.FromBase64String(headerParser.Cypher), encryptionKey);
                    string decryptedValue = Encoding.UTF8.GetString(decryptBytes);
                    return decryptedValue;
                }
            }

            return stringToDecrypt;
        }

        /// <inheritdoc/>
        public T EncryptObject<T>(T toEncrypt)
        {
            if (CurrentEncryptionKey != null && CurrentEncryptionKey.IsKeyReadyToUse)
            {
                // Get all the properties that are encryptable and encrypt them.
                IEnumerable<PropertyInfo> encryptedProperties = toEncrypt.GetType()
                    .GetProperties().Where(p => p.GetCustomAttributes(typeof(EncryptedAttribute), true)
                    .Any(a => p.PropertyType == typeof(string)));

                EncryptedFieldHeaderParser headerParser;
                foreach (PropertyInfo encryptedPropertyInfo in encryptedProperties.ToList())
                {
                    string propertyValue = encryptedPropertyInfo.GetValue(toEncrypt) as string;
                    if (!string.IsNullOrEmpty(propertyValue))
                    {
                        headerParser = new EncryptedFieldHeaderParser(propertyValue);
                        if (!headerParser.IsEncrypted)
                        {
                            string encryptedValue = Encrypt(propertyValue, CurrentEncryptionKey);
                            encryptedPropertyInfo.SetValue(toEncrypt, encryptedValue);
                        }
                    }
                }
            }

            return toEncrypt;
        }

        /// <inheritdoc/>
        public T DecryptObject<T>(T toDecrypt)
        {
            DecryptObjectImpl(toDecrypt, false);
            return toDecrypt;
        }

        /// <inheritdoc/>
        public T DecryptEntityFrameworkObject<T>(T toDecrypt, [CallerFilePath] string callerClass = "")
        {
            // check here if this is called from PlatformDbContext
            if (!string.IsNullOrEmpty(callerClass) &&
                (!callerClass.Contains("PlatformDbContext", StringComparison.OrdinalIgnoreCase) && !callerClass.Contains("DapperExtensions", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ExosPersistenceException("Invalid caller for this method, can be called only from PlatformDbContext or DapperExtensions");
            }

            DecryptObjectImpl(toDecrypt, true);
            return toDecrypt;
        }

        /// <inheritdoc/>
        public IEnumerable<T> DecryptEnumerable<T>(IEnumerable<T> toDecrypt)
        {
            if (toDecrypt != null && toDecrypt.Any())
            {
                foreach (var result in toDecrypt)
                {
                    DecryptObject<T>(result);
                }
            }

            return toDecrypt;
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytesToEncrypt, EncryptionKey encryptionKey = null)
        {
            encryptionKey ??= CurrentEncryptionKey;
            if (encryptionKey.IsKeyReadyToUse)
            {
                byte[] encryptedBytes = EncryptBytes(bytesToEncrypt, encryptionKey);
                return encryptedBytes;
            }

            return bytesToEncrypt;
        }

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] encryptedBytes, EncryptionKey encryptionKey = null)
        {
            encryptionKey ??= CurrentEncryptionKey;
            encryptionKey = ValidateDecryptionKey(encryptionKey);
            if (encryptionKey.IsKeyReadyToUse)
            {
                byte[] decryptedBytes = DecryptBytes(encryptedBytes, encryptionKey);
                return decryptedBytes;
            }

            return encryptedBytes;
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            byte[] output = new byte[first.Length + second.Length];

            for (int i = 0; i < first.Length; i++)
            {
                output[i] = first[i];
            }

            for (int j = 0; j < second.Length; j++)
            {
                output[first.Length + j] = second[j];
            }

            return output;
        }

        private static byte[] SubArray(byte[] data, int start, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, start, result, 0, length);
            return result;
        }

        private static byte[] EncryptBytes(byte[] bytesToEncrypt, EncryptionKey encryptionKey, byte[] associatedData = null)
        {
            if (bytesToEncrypt == null)
            {
                throw new ArgumentNullException(nameof(bytesToEncrypt));
            }

            if (encryptionKey == null)
            {
                throw new ArgumentNullException(nameof(encryptionKey));
            }

            byte[] tag = new byte[_tagBytes];

            // Generate Random Nonce
            using var cyptoServiceProvider = RandomNumberGenerator.Create();
            byte[] nonce = new byte[_nonceBytes];
            cyptoServiceProvider.GetBytes(nonce);

            byte[] cipherBytes = new byte[bytesToEncrypt.Length];

            using AesGcm cipher = new AesGcm(encryptionKey.KeyValueBytes);
            cipher.Encrypt(nonce, bytesToEncrypt, cipherBytes, tag, associatedData);
            return Concat(tag, Concat(nonce, cipherBytes));
        }

        private static byte[] DecryptBytes(byte[] cipherBytes, EncryptionKey encryptionKey, byte[] associatedData = null)
        {
            if (cipherBytes == null)
            {
                throw new ArgumentNullException(nameof(cipherBytes));
            }

            if (encryptionKey == null)
            {
                throw new ArgumentNullException(nameof(encryptionKey));
            }

            byte[] tag = SubArray(cipherBytes, 0, _tagBytes);
            byte[] nonce = SubArray(cipherBytes, _tagBytes, _nonceBytes);

            byte[] toDecrypt = SubArray(cipherBytes, _tagBytes + _nonceBytes, cipherBytes.Length - tag.Length - nonce.Length);
            byte[] decryptedData = new byte[toDecrypt.Length];

            using var cipher = new AesGcm(encryptionKey.KeyValueBytes);
            cipher.Decrypt(nonce, toDecrypt, tag, decryptedData, associatedData);
            return decryptedData;
        }

        private T DecryptObjectImpl<T>(T toDecrypt, bool suppressValidation)
        {
            if (toDecrypt is IAuditable && !suppressValidation)
            {
                throw new ExosPersistenceException("Decryption of Entity Framework objects is not allowed");
            }

            // Get all the properties that are encryptable and decrypt them.
            IEnumerable<PropertyInfo> encryptedProperties = toDecrypt.GetType()
                .GetProperties().Where(p => p.GetCustomAttributes(typeof(EncryptedAttribute), true)
                .Any(a => p.PropertyType == typeof(string)));
            foreach (PropertyInfo encryptedPropertyInfo in encryptedProperties.ToList())
            {
                string encryptedPropertyValue = encryptedPropertyInfo.GetValue(toDecrypt) as string;
                if (!string.IsNullOrEmpty(encryptedPropertyValue))
                {
                    string decryptedValue = Decrypt(encryptedPropertyValue);
                    encryptedPropertyInfo.SetValue(toDecrypt, decryptedValue);
                }
            }

            return toDecrypt;
        }
    }
}
