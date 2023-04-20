namespace Exos.Platform.AspNetCore.Encryption
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Implementation of the Advanced Encryption Standard (AES)
    /// symmetric algorithm.
    /// </summary>
    public class AesEncryption : ISecureEncryption
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesEncryption"/> class.
        /// </summary>
        public AesEncryption()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AesEncryption"/> class.
        /// </summary>
        /// <param name="configuration">IConfiguration.</param>
        public AesEncryption(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Decrypt a string using a key and initialization vector.
        /// </summary>
        /// <param name="encryptedText">Encrypted Text.</param>
        /// <param name="pvtKey">Decryption Key.</param>
        /// <returns>Decrypted Text.</returns>
        public static string Decrypt(string encryptedText, string pvtKey)
        {
            var cypherText = Convert.FromBase64String(encryptedText);
            var iv = new byte[16];
            var cipher = new byte[cypherText.Length - iv.Length];
            Buffer.BlockCopy(cypherText, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(cypherText, iv.Length, cipher, 0, cypherText.Length - iv.Length);
            var key = Encoding.UTF8.GetBytes(pvtKey);
            using (var aesAlgorithm = Aes.Create() ?? throw new InvalidOperationException("Aes.Create() returns null."))
            {
                using (var decryption = aesAlgorithm.CreateDecryptor(key, iv))
                {
                    string result;
                    using (var memoryStreamDecrypt = new MemoryStream(cipher))
                    {
                        using (var cryptoStreamDecrypt = new CryptoStream(memoryStreamDecrypt, decryption, CryptoStreamMode.Read))
                        {
                            using (var streamReaderDecrypt = new StreamReader(cryptoStreamDecrypt))
                            {
                                result = streamReaderDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Decrypt a base64 encoded string using a key and initialization vector.
        /// </summary>
        /// <param name="stringToEncrypt">String to Encrypt.</param>
        /// <param name="pvtKey">Encryption Key.</param>
        /// <returns>Encrypted String.</returns>
        public static string Encrypt(string stringToEncrypt, string pvtKey)
        {
            var key = Encoding.UTF8.GetBytes(pvtKey);
            using (var aesAlgorithm = Aes.Create() ?? throw new InvalidOperationException("Aes.Create() returns null."))
            {
                using (var encryption = aesAlgorithm.CreateEncryptor(key, aesAlgorithm.IV))
                {
                    using (var memoryStreamEncrypt = new MemoryStream())
                    {
                        using (var cryptoStreamEncrypt = new CryptoStream(memoryStreamEncrypt, encryption, CryptoStreamMode.Write))
                        using (var streamReaderEncrypt = new StreamWriter(cryptoStreamEncrypt))
                        {
                            streamReaderEncrypt.Write(stringToEncrypt);
                        }

                        var iv = aesAlgorithm.IV;
                        var decryptedContent = memoryStreamEncrypt.ToArray();
                        var result = new byte[iv.Length + decryptedContent.Length];
                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);
                        return Convert.ToBase64String(result);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public string EncryptPiiData(string plainText, string keyName)
        {
            if (keyName == null)
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            string keyVersion = _configuration[keyName + "-Version"];
            var keyValue = keyVersion == null ? _configuration[keyName] : _configuration[keyName + "-" + keyVersion];
            if (keyValue == null)
            {
                throw new InvalidOperationException("Encryption key is null from the configuration");
            }

            return Encrypt(plainText, keyValue) + "|" + keyName + "-" + keyVersion;
        }

        /// <inheritdoc/>
        public string DecriptPiiData(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                throw new ArgumentNullException(nameof(encryptedText));
            }

            // Find the key from the string. Pass the key to decrypt
            var textWithKey = encryptedText.Split('|');

            // Find key from the vault, using the key
            string keyFromVault = _configuration[textWithKey[1]];
            if (textWithKey.Length != 2)
            {
                throw new InvalidOperationException($"Encrypted Text is not in correct format! {encryptedText}");
            }

            return Decrypt(textWithKey[0], keyFromVault);
        }
    }
}
