namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="AesCbcBlobEncryption" />.
    /// Advanced Encryption Standard (AES)  with the CBC (Cipher Blocker Chaining) mode of operation.
    /// Use the ClientKeyIdentifier from the user context to find the key for encryption / decryption.
    /// </summary>
    public class AesCbcBlobEncryption : AEncryptionCommon, IBlobEncryption
    {
        private readonly ILogger<AesCbcBlobEncryption> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesCbcBlobEncryption"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{AesCbcBlobEncryption}"/>.</param>
        /// <param name="encryptionKeyFinder">The encryptionKeyFinder<see cref="IEncryptionKeyFinder"/>.</param>
        public AesCbcBlobEncryption(ILogger<AesCbcBlobEncryption> logger, IEncryptionKeyFinder encryptionKeyFinder) : base(encryptionKeyFinder, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool IsEncrypted(IDictionary<string, string> fileMetadata)
        {
            _ = fileMetadata ?? throw new ArgumentNullException(nameof(fileMetadata));

            if (!fileMetadata.TryGetValue(IBlobEncryption.BlobEncryptionKeyName, out var keyName) || string.IsNullOrEmpty(keyName))
            {
                return false;
            }
            else if (!fileMetadata.TryGetValue(IBlobEncryption.BlobEncryptionKeyVersion, out var keyVersion) || string.IsNullOrEmpty(keyVersion))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Encrypt(Stream streamToEncrypt, Stream encryptedStream, out Dictionary<string, string> fileMetadata, EncryptionKey encryptionKey = null)
        {
            if (streamToEncrypt == null)
            {
                throw new ArgumentNullException(nameof(streamToEncrypt));
            }

            if (encryptedStream == null)
            {
                throw new ArgumentNullException(nameof(encryptedStream));
            }

            encryptionKey ??= CurrentEncryptionKey;
            fileMetadata = new Dictionary<string, string>();

            if (encryptionKey.IsKeyReadyToUse)
            {
                EncryptStream(streamToEncrypt, encryptedStream, encryptionKey, fileMetadata);
                fileMetadata.Add(IBlobEncryption.BlobEncryptionKeyName, encryptionKey.KeyName);
                fileMetadata.Add(IBlobEncryption.BlobEncryptionKeyVersion, encryptionKey.KeyVersion);
                if (streamToEncrypt.CanSeek)
                {
                    fileMetadata.Add(IBlobEncryption.UnencryptedContentLengthKey, streamToEncrypt.Length.ToString(CultureInfo.InvariantCulture));
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Decrypt(Stream encryptedStream, Stream decryptedStream, IDictionary<string, string> fileMetadata)
        {
            if (encryptedStream == null)
            {
                throw new ArgumentNullException(nameof(encryptedStream));
            }

            if (decryptedStream == null)
            {
                throw new ArgumentNullException(nameof(decryptedStream));
            }

            if (fileMetadata != null && fileMetadata.Any())
            {
                // Create the decryption key from the file metadata
                EncryptionKey encryptionKey = new EncryptionKey();
                if (fileMetadata.TryGetValue(IBlobEncryption.BlobEncryptionKeyName, out string blobEncryptionKeyName))
                {
                    encryptionKey.KeyName = blobEncryptionKeyName;
                }

                if (fileMetadata.TryGetValue(IBlobEncryption.BlobEncryptionKeyVersion, out string blobEncryptionVersion))
                {
                    encryptionKey.KeyVersion = blobEncryptionVersion;
                }

                if (fileMetadata.TryGetValue(IBlobEncryption.HMAC, out string hmacBase64))
                {
                    encryptionKey.HmacBase64 = hmacBase64;
                }

                if (encryptionKey.IsHeaderComplete)
                {
                    return Decrypt(encryptedStream, decryptedStream, encryptionKey);
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Decrypt(Stream encryptedStream, Stream decryptedStream, EncryptionKey encryptionKey = null)
        {
            if (encryptedStream == null)
            {
                throw new ArgumentNullException(nameof(encryptedStream));
            }

            if (decryptedStream == null)
            {
                throw new ArgumentNullException(nameof(decryptedStream));
            }

            encryptionKey ??= CurrentEncryptionKey;
            encryptionKey = ValidateDecryptionKey(encryptionKey);
            if (encryptionKey.IsKeyReadyToUse)
            {
                using var aesCryptoServiceProvider = Aes.Create();
                aesCryptoServiceProvider.Mode = CipherMode.CBC;
                aesCryptoServiceProvider.Key = encryptionKey.KeyValueBytes;
                aesCryptoServiceProvider.Padding = PaddingMode.PKCS7;

                byte[] iv = new byte[aesCryptoServiceProvider.IV.Length];

                // Read the IV from the encrypted Stream.
                encryptedStream.Read(iv, 0, iv.Length);

                using HMACSHA256 hmacsha256 = new HMACSHA256(encryptionKey.KeyValueBytes);
                byte[] hash256 = new byte[hmacsha256.HashSize / 8];

                if (string.IsNullOrEmpty(encryptionKey.HmacBase64))
                {
                    // Read the HMAC/TAG from the encrypted stream
                    encryptedStream.Read(hash256, 0, hmacsha256.HashSize / 8);
                }
                else
                {
                    // HMAC/TAG is in the metadata copy the bytes to the hash256 byte array
                    hash256 = Convert.FromBase64String(encryptionKey.HmacBase64);
                }

                using (ICryptoTransform decryptor = aesCryptoServiceProvider.CreateDecryptor(encryptionKey.KeyValueBytes, iv))
                using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read, true))
                {
                    cryptoStream.CopyTo(decryptedStream);
                }

                decryptedStream.Position = 0;
                // Generate Hash from Decrypted stream and compare
                byte[] computedHash256 = hmacsha256.ComputeHash(decryptedStream);

                if (!hash256.SequenceEqual(computedHash256))
                {
                    throw new ExosPersistenceException("Invalid HMAC/TAG in Stream.");
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public EncryptedBlobStream GetDecryptStream(Stream encryptedStream, IDictionary<string, string> fileMetadata)
        {
            _ = encryptedStream ?? throw new ArgumentNullException(nameof(encryptedStream));
            _ = fileMetadata ?? throw new ArgumentNullException(nameof(fileMetadata));

            // Caller should have already checked file metadata with IsEncrypted()
            fileMetadata.TryGetValue(IBlobEncryption.BlobEncryptionKeyName, out var keyName);
            fileMetadata.TryGetValue(IBlobEncryption.BlobEncryptionKeyVersion, out var keyVersion);
            fileMetadata.TryGetValue(IBlobEncryption.HMAC, out var hmacBase64);

            var encryptionKey = new EncryptionKey
            {
                KeyName = keyName,
                KeyVersion = keyVersion,
                HmacBase64 = hmacBase64
            };

            // Resolve the encryption key
            encryptionKey = ValidateDecryptionKey(encryptionKey);

            return EncryptedBlobStream.CreateRead(encryptedStream, encryptionKey);
        }

        private static void EncryptStream(Stream streamToEncrypt, Stream encryptedStream, EncryptionKey encryptionKey, IDictionary<string, string> fileMetadata)
        {
            if (streamToEncrypt == null)
            {
                throw new ArgumentNullException(nameof(streamToEncrypt));
            }

            if (encryptedStream == null)
            {
                throw new ArgumentNullException(nameof(streamToEncrypt));
            }

            streamToEncrypt.Position = 0;

            using var aesCryptoServiceProvider = Aes.Create();
            aesCryptoServiceProvider.Mode = CipherMode.CBC;
            aesCryptoServiceProvider.Key = encryptionKey.KeyValueBytes;
            aesCryptoServiceProvider.Padding = PaddingMode.PKCS7;

            // Generate IV
            aesCryptoServiceProvider.GenerateIV();

            // Generate HMAC/TAG
            using HMACSHA256 hmacsha256 = new HMACSHA256(encryptionKey.KeyValueBytes);
            byte[] hash256 = hmacsha256.ComputeHash(streamToEncrypt);
            fileMetadata.Add(IBlobEncryption.HMAC, Convert.ToBase64String(hash256));
            streamToEncrypt.Position = 0;

            using ICryptoTransform encryptor = aesCryptoServiceProvider.CreateEncryptor();
            using CryptoStream cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write, true);
            // Write the IV to the encrypted stream
            encryptedStream.Write(aesCryptoServiceProvider.IV, 0, aesCryptoServiceProvider.IV.Length);

            // #416573 Latest Version don't add the hmac in stream, it's returned in the metadata
            // Write the HMAC/TAG to the encrypted stream
            // encryptedStream.Write(hash256, 0, hash256.Length);

            streamToEncrypt.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
        }
    }
}