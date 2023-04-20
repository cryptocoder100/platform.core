namespace Exos.Platform.Persistence.Encryption
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Implements Encryption / Decryption for Blob Objects.
    /// </summary>
    public interface IBlobEncryption
    {
        /// <summary>
        /// The key name in blob objects.
        /// </summary>
        public const string BlobEncryptionKeyName = "BlobEncryptionKeyName";

        /// <summary>
        /// The key version in blob objects.
        /// </summary>
        public const string BlobEncryptionKeyVersion = "BlobEncryptionKeyVersion";

        /// <summary>
        /// The header/metadata key name for specifying the unencrypted content length.
        /// </summary>
        public const string UnencryptedContentLengthKey = "UnencryptedContentLength";

        /// <summary>
        /// Hash-based Message Authentication Code.
        /// </summary>
        public const string HMAC = "HMAC";

        /// <summary>
        /// Gets or sets the current encryption key.
        /// </summary>
        public EncryptionKey CurrentEncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the request
        /// validates the key for decryption if is set to false
        /// the request can decrypt any record.
        /// </summary>
        public bool ValidateKeyForDecryption { get; set; }

        /// <summary>
        /// Set the encryption key to use.
        /// </summary>
        /// <param name="keyIdentifier">Key identifier, to find the key name in the EncryptionKeyMappingConfiguration.</param>
        public EncryptionKey SetEncryptionKey(string keyIdentifier);

        /// <summary>
        /// Indicates whether a blob is encrypted by inspecting the file metadata for the
        /// <see cref="BlobEncryptionKeyName" /> and <see cref="BlobEncryptionKeyVersion" /> keys.
        /// </summary>
        /// <param name="fileMetadata">The file metadata.</param>
        /// <returns><c>true</c> if the blob is encrypted; otherwise, <c>false</c>.</returns>
        public bool IsEncrypted(IDictionary<string, string> fileMetadata);

        /// <summary>
        /// Encrypt the Stream.
        /// </summary>
        /// <param name="streamToEncrypt">Stream to encrypt.</param>
        /// <param name="encryptedStream">Encrypted stream.</param>
        /// <param name="fileMetadata">File Metadata, contains key name and version, this can be stored in Blob Service and used later for decryption.</param>
        /// <param name="encryptionKey"><see cref="EncryptionKey"/>If is null default encryption key is used.</param>
        /// <returns>True if Stream is encrypted, false otherwise.</returns>
        public bool Encrypt(Stream streamToEncrypt, Stream encryptedStream, out Dictionary<string, string> fileMetadata, EncryptionKey encryptionKey = null);

        /// <summary>
        /// Decrypt the Stream.
        /// </summary>
        /// <param name="encryptedStream">Encrypted Stream.</param>
        /// <param name="decryptedStream">Decrypted Stream.</param>
        /// <param name="fileMetadata">File Metadata, should contain the key name and version.</param>
        /// <returns>True if Stream is decrypted, false otherwise.</returns>
        public bool Decrypt(Stream encryptedStream, Stream decryptedStream, IDictionary<string, string> fileMetadata);

        /// <summary>
        /// Decrypt the Stream.
        /// </summary>
        /// <param name="encryptedStream">Encrypted Stream.</param>
        /// <param name="decryptedStream">Decrypted Stream.</param>
        /// <param name="encryptionKey"><see cref="EncryptionKey"/>.</param>
        /// <returns>True if Stream is decrypted, false otherwise.</returns>
        public bool Decrypt(Stream encryptedStream, Stream decryptedStream, EncryptionKey encryptionKey = null);

        /// <summary>
        /// Gets a stream that can be used to decrypt an encrypted blob stream.
        /// </summary>
        /// <param name="encryptedStream">The encrypted blob stream.</param>
        /// <param name="fileMetadata">The blob metadata.</param>
        /// <returns>A decryption stream.</returns>
        /// <remarks>
        /// Use the <see cref="IsEncrypted(IDictionary{string, string})" /> method to determine whether the blob is encrypted prior to calling <see cref="GetDecryptStream(Stream, IDictionary{string, string})" />.
        /// Attempting to decrypt a blob stream that is not encrypted will result in an exception.
        /// </remarks>
        public EncryptedBlobStream GetDecryptStream(Stream encryptedStream, IDictionary<string, string> fileMetadata);
    }
}
