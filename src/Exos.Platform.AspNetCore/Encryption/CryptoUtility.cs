namespace Exos.Platform.AspNetCore.Encryption
{
    using Microsoft.Extensions.Configuration;

    /// <inheritdoc/>
    public class CryptoUtility : IEncryption
    {
        private const string PrivateKey = "ExOs2SoXA$wKklmn";
        private readonly IConfiguration _configuration;
        private readonly AesEncryption _aesEncryption;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoUtility"/> class.
        /// </summary>
        public CryptoUtility()
        {
            _aesEncryption = new AesEncryption();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoUtility"/> class.
        /// </summary>
        /// <param name="configuration">IConfiguration.</param>
        public CryptoUtility(IConfiguration configuration)
        {
            _configuration = configuration;
            _aesEncryption = new AesEncryption(configuration);
        }

        /// <inheritdoc/>
        public string Encrypt(string stringToEncrypt)
        {
            return AesEncryption.Encrypt(stringToEncrypt, PrivateKey);
        }

        /// <inheritdoc/>
        public string Decrypt(string encryptedString)
        {
            return AesEncryption.Decrypt(encryptedString, PrivateKey);
        }
    }
}