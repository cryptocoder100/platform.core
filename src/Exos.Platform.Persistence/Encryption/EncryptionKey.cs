#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable SA1204 // Static elements should appear before instance elements
namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Text;

    /// <summary>
    /// Represent a Client Encryption Key.
    /// </summary>
    public class EncryptionKey
    {
        /// <summary>
        /// Gets or sets the Key Identifier
        /// the request subdomain like api.exostechnology.com.
        /// </summary>
        public string KeyIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the Key Name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the encryption Key as string.
        /// Key length is can be  16, 24, or 32 bytes (128, 192, or 256 bits).
        /// </summary>
        public string KeyValue { get; set; }

        /// <summary>
        /// Gets or sets the encryption Key as byte[].
        /// Key length is can be  16, 24, or 32 bytes (128, 192, or 256 bits).
        /// </summary>
        public byte[] KeyValueBytes { get; set; }

        /// <summary>
        /// Gets or sets the Key Version.
        /// Key vault version use a GUID.
        /// </summary>
        public string KeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the HmacBase64.
        /// Hash-based Message Authentication Code (HMAC) in base64 encoding.
        /// Used for blob decryption.
        /// The value is returned in the blob metadata on encryption.
        /// </summary>
        public string HmacBase64 { get; set; }

        /// <summary>
        /// Gets or sets the  KeyNameBase.
        /// </summary>
        public string KeyNameBase { get; set; }

        /// <summary>
        /// Gets a value indicating whether the key is ready for use or not.
        /// </summary>
        /// <returns>True if valid, False otherwise.</returns>
        public bool IsKeyReadyToUse
        {
            get
            {
                if (IsHeaderComplete && IsKeyValueValid)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the values of Client Key Name and Version are set.
        /// </summary>
        public bool IsHeaderComplete
        {
            get
            {
                if (!string.IsNullOrEmpty(KeyName) && !string.IsNullOrEmpty(KeyVersion))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the key value is valid or not.
        /// </summary>
        /// <returns>True if key value is valid, False otherwise.</returns>
        public bool IsKeyValueValid
        {
            get
            {
                if (KeyValueBytes != null && KeyValueBytes.Length == IDatabaseEncryption.EncryptionKeyLengthBits / 8)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the formatted encryption header string.
        /// </summary>
        /// <returns>The properly formatted encryption header string.</returns>
        public string EncryptedHeaderString => $"{KeyName}{IDatabaseEncryption.EncryptedHeaderDelimiter}{KeyVersion}";

        /// <summary>
        /// Takes in the cypher string and builds the final field value that should be stored in the datastore.
        /// </summary>
        /// <param name="cypherString">The encrypted text to append.</param>
        /// <returns>The fully assembled field value.</returns>
        public string AssembleFinalFieldValue(string cypherString)
        {
            return $"{EncryptedHeaderString}{IDatabaseEncryption.EncryptedHeaderDelimiter}{cypherString}";
        }

        /// <summary>
        /// Determines whether this ClientEncryptionKey and a specified ClientEncryptionKey object have the
        /// same Key Name and Key Version.
        /// </summary>
        /// <param name="encryptionKey">ClientEncryptionKey to compare.</param>
        /// <returns>True if Key Name and Version match, false otherwise.</returns>
        public bool Equals(EncryptionKey encryptionKey)
        {
            if (encryptionKey != null &&
                string.Equals(encryptionKey.KeyName, KeyName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(encryptionKey.KeyVersion, KeyVersion, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the bytes from the key value string
        /// validate if the key is a base64 string.
        /// </summary>
        /// <param name="keyValue">Key value.</param>
        /// <returns>key Value bytes.</returns>
        public static byte[] GetKeyBytes(string keyValue)
        {
            if (!string.IsNullOrEmpty(keyValue))
            {
                var keyBytes = new Span<byte>(new byte[IDatabaseEncryption.EncryptionKeyLengthBits / 8]);
                // Check if key is base64 string.
                if (Convert.TryFromBase64String(keyValue, keyBytes, out _))
                {
                    return keyBytes.ToArray();
                }
                else
                {
                    return Encoding.UTF8.GetBytes(keyValue);
                }
            }

            return Array.Empty<byte>();
        }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays
#pragma warning restore SA1204 // Static elements should appear before instance elements