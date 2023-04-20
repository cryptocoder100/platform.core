namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Generate new keys for encryption.
    /// </summary>
    public static class EncryptionKeyGenerator
    {
        /// <summary>
        /// Generates a new key and returns it as a byte array.
        /// </summary>
        /// <returns>Byte array with new key.</returns>
        public static byte[] NewKeyBytes()
        {
            using var cyptoServiceProvider = RandomNumberGenerator.Create();
            byte[] key = new byte[IDatabaseEncryption.EncryptionKeyLengthBits / 8];
            cyptoServiceProvider.GetBytes(key);
            return key;
        }

        /// <summary>
        /// Generates a new key and returns it as a Base64 string.
        /// </summary>
        /// <returns>base64 string with new key.</returns>
        public static string NewKeyEncoded()
        {
            return EncodeKey(NewKeyBytes());
        }

        private static string EncodeKey(byte[] keyBytes)
        {
            return Convert.ToBase64String(keyBytes);
        }
    }
}
