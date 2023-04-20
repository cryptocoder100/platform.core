namespace Exos.Platform.Persistence.Encryption
{
    using System.Collections.Generic;

    /// <summary>
    /// Implements Encryption / Decryption
    /// for database, SQL and Cosmos.
    /// </summary>
    public interface IDatabaseEncryption
    {
        /// <summary>
        /// Standard delimiter used for separating the secret information from the cypher.
        /// </summary>
        public const string EncryptedHeaderDelimiter = "|";

        /// <summary>
        /// The standard required length of encryption Keys in bytes.
        /// </summary>
        public const int EncryptionKeyLengthBits = 256;

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
        /// <param name="keyIdentifier">The keyIdentifier<see cref="string"/> to find the key name in the EncryptionKeyMappingConfiguration.</param>
        /// <returns>Client Encryption Key.</returns>
        public EncryptionKey SetEncryptionKey(string keyIdentifier);

        /// <summary>
        ///  Set the encryption key to use.
        /// </summary>
        /// <param name="encryptionKey">The encryptionKey<see cref="EncryptionKey"/>.</param>
        /// <returns>The <see cref="EncryptionKey"/>.</returns>
        public EncryptionKey SetEncryptionKey(EncryptionKey encryptionKey);

        /// <summary>
        /// Encrypts a string.
        /// </summary>
        /// <param name="stringToEncrypt">String to encrypt.</param>
        /// <param name="encryptionKey"><see cref="EncryptionKey"/>.</param>
        /// <param name="concatenateKey">True to concatenante the Key Name and Key Version to the encrypted value.</param>
        /// <returns>Encrypted string.</returns>
        public string Encrypt(string stringToEncrypt, EncryptionKey encryptionKey = null, bool concatenateKey = true);

        /// <summary>
        /// Decrypts a string.
        /// </summary>
        /// <param name="stringToDecrypt">Encrypted string to decrypt.</param>
        /// <returns>Decrypted string.</returns>
        public string Decrypt(string stringToDecrypt);

        /// <summary>
        /// Encrypt an object with Encrypted (EncryptedAttribute) properties, an entity or model.
        /// </summary>
        /// <typeparam name="T">Object Type to encrypt.</typeparam>
        /// <param name="toEncrypt">Object to encrypt.</param>
        /// <returns>Encrypted object.</returns>
        public T EncryptObject<T>(T toEncrypt);

        /// <summary>
        /// Decrypt an object with Encrypted (EncryptedAttribute) properties.
        /// </summary>
        /// <typeparam name="T">Object Type to decrypt.</typeparam>
        /// <param name="toDecrypt">Object to decrypt.</param>
        /// <returns>Decrypted object.</returns>
        public T DecryptObject<T>(T toDecrypt);

        /// <summary>
        /// Decrypt an Entity Framework with Encrypted (EncryptedAttribute) properties.
        /// Called only from PlatformDbContext or DapperExtensions if is called from a different class throw an exception.
        /// </summary>
        /// <typeparam name="T">Object Type to decrypt.</typeparam>
        /// <param name="toDecrypt">Object to decrypt.</param>
        /// <param name="callerClass">Class that calls this method.</param>
        /// <returns>Decrypted object.</returns>
        public T DecryptEntityFrameworkObject<T>(T toDecrypt, [System.Runtime.CompilerServices.CallerFilePathAttribute] string callerClass = "");

        /// <summary>
        /// Decrypt an Enumerable Object.
        /// </summary>
        /// <typeparam name="T">Object Type to encrypt.</typeparam>
        /// <param name="toDecrypt">Object to decrypt <see cref="IEnumerable{T}"/>.</param>
        /// <returns>The <see cref="IEnumerable{T}"/> decrypted.</returns>
        public IEnumerable<T> DecryptEnumerable<T>(IEnumerable<T> toDecrypt);

        /// <summary>
        /// Encrypt the byte array.
        /// </summary>
        /// <param name="bytesToEncrypt">Byte array to encrypt.</param>
        /// <param name="encryptionKey"><see cref="EncryptionKey"/>.</param>
        /// <returns>Encrypted byte array.</returns>
        public byte[] Encrypt(byte[] bytesToEncrypt, EncryptionKey encryptionKey = null);

        /// <summary>
        /// Decrypt the byte array.
        /// </summary>
        /// <param name="encryptedBytes">Encrypted byte array.</param>
        /// <param name="encryptionKey"><see cref="EncryptionKey"/>.</param>
        /// <returns>Decrypted byte array.</returns>
        public byte[] Decrypt(byte[] encryptedBytes, EncryptionKey encryptionKey = null);
    }
}