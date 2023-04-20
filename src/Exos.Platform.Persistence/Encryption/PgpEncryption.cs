namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.IO;
    using Org.BouncyCastle.Bcpg;
    using Org.BouncyCastle.Bcpg.OpenPgp;
    using PgpCore;

    /// <inheritdoc/>
    public class PgpEncryption : IPgpEncryption
    {
        private readonly ILogger<PgpEncryption> _logger;
        private readonly IEncryptionKeyFinder _encryptionKeyFinder;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PgpEncryption"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{PgpEncryption}"/>.</param>
        /// <param name="encryptionKeyFinder">The encryptionKeyFinder<see cref="IEncryptionKeyFinder"/>.</param>
        /// <param name="recyclableMemoryStreamManager">The recyclableMemoryStreamManager<see cref="RecyclableMemoryStreamManager"/>.</param>
        public PgpEncryption(ILogger<PgpEncryption> logger, IEncryptionKeyFinder encryptionKeyFinder, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptionKeyFinder = encryptionKeyFinder ?? throw new ArgumentNullException(nameof(encryptionKeyFinder));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager ?? throw new ArgumentNullException(nameof(recyclableMemoryStreamManager));
        }

        /// <inheritdoc/>
        public async Task EncryptFileAsync(string inputFilePath, string outputFilePath, string publicKeyName)
        {
            string publicKeyValue = GetPgpKeyValue(publicKeyName);
            using MemoryStream publicKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(publicKeyValue));
            using Stream inputFileStream = File.OpenRead(inputFilePath);
            using Stream outputFileStream = File.Create(outputFilePath);
            using PGP pgp = GetPGPInstance();
            // Encrypt the file
            await pgp.EncryptStreamAsync(inputFileStream, outputFileStream, publicKeyStream);
        }

        /// <inheritdoc/>
        public async Task EncryptStreamAsync(Stream inputStream, Stream outputStream, string publicKeyName)
        {
            string publicKeyValue = GetPgpKeyValue(publicKeyName);
            using MemoryStream publicKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(publicKeyValue));
            using PGP pgp = GetPGPInstance();
            // Encrypt the stream
            await pgp.EncryptStreamAsync(inputStream, outputStream, publicKeyStream);
            if (outputStream != null && outputStream.Length > 0)
            {
                outputStream.Position = 0;
            }
        }

        /// <inheritdoc/>
        public async Task EncryptFileAndSignAsync(string inputFilePath, string outputFilePath, string publicKeyName, string privateKeyName, string passPhrase = "")
        {
            string publicKeyValue = GetPgpKeyValue(publicKeyName);
            string privateKeyValue = GetPgpKeyValue(privateKeyName);
            string passPhraseValue = string.Empty;
            if (!string.IsNullOrEmpty(passPhrase))
            {
                passPhraseValue = GetPgpPassPhrase(passPhrase);
            }

            using MemoryStream publicKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(publicKeyValue));
            using MemoryStream privateKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(privateKeyValue));

            using Stream inputFileStream = File.OpenRead(inputFilePath);
            using Stream outputFileStream = File.Create(outputFilePath);
            using PGP pgp = GetPGPInstance();
            // Encrypt the file
            await pgp.EncryptStreamAndSignAsync(inputFileStream, outputFileStream, publicKeyStream, privateKeyStream, passPhraseValue);
        }

        /// <inheritdoc/>
        public async Task EncryptStreamAndSignAsync(Stream inputStream, Stream outputStream, string publicKeyName, string privateKeyName, string passPhrase = "")
        {
            string publicKeyValue = GetPgpKeyValue(publicKeyName);
            string privateKeyValue = GetPgpKeyValue(privateKeyName);
            string passPhraseValue = string.Empty;
            if (!string.IsNullOrEmpty(passPhrase))
            {
                passPhraseValue = GetPgpPassPhrase(passPhrase);
            }

            using MemoryStream publicKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(publicKeyValue));
            using MemoryStream privateKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(privateKeyValue));

            using PGP pgp = GetPGPInstance();
            // Encrypt the Stream
            await pgp.EncryptStreamAndSignAsync(inputStream, outputStream, publicKeyStream, privateKeyStream, passPhraseValue);
            if (outputStream != null && outputStream.Length > 0)
            {
                outputStream.Position = 0;
            }
        }

        /// <inheritdoc/>
        public async Task DecryptFileAsync(string inputFilePath, string outputFilePath, string privateKeyName, string passPhrase = "")
        {
            string privateKeyValue = GetPgpKeyValue(privateKeyName);
            using MemoryStream privateKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(privateKeyValue));

            string passPhraseValue = string.Empty;
            if (!string.IsNullOrEmpty(passPhrase))
            {
                passPhraseValue = GetPgpPassPhrase(passPhrase);
            }

            using Stream inputFileStream = File.OpenRead(inputFilePath);
            using Stream outputFileStream = File.Create(outputFilePath);
            using PGP pgp = GetPGPInstance();
            await pgp.DecryptStreamAsync(inputFileStream, outputFileStream, privateKeyStream, passPhraseValue);
        }

        /// <inheritdoc/>
        public async Task DecryptStreamAsync(Stream inputStream, Stream outputStream, string privateKeyName, string passPhrase = "")
        {
            string privateKeyValue = GetPgpKeyValue(privateKeyName);
            using MemoryStream privateKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(privateKeyValue));

            string passPhraseValue = string.Empty;
            if (!string.IsNullOrEmpty(passPhrase))
            {
                passPhraseValue = GetPgpPassPhrase(passPhrase);
            }

            using PGP pgp = GetPGPInstance();
            await pgp.DecryptStreamAsync(inputStream, outputStream, privateKeyStream, passPhraseValue);
            if (outputStream != null && outputStream.Length > 0)
            {
                outputStream.Position = 0;
            }
        }

        /// <inheritdoc/>
        public async Task DecryptFileAndVerifyAsync(string inputFilePath, string outputFilePath, string publicKeyName, string privateKeyName, string passPhrase = "")
        {
            string publicKeyValue = GetPgpKeyValue(publicKeyName);
            string privateKeyValue = GetPgpKeyValue(privateKeyName);
            string passPhraseValue = string.Empty;
            if (!string.IsNullOrEmpty(passPhrase))
            {
                passPhraseValue = GetPgpPassPhrase(passPhrase);
            }

            using MemoryStream publicKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(publicKeyValue));
            using MemoryStream privateKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(privateKeyValue));
            using Stream inputFileStream = File.OpenRead(inputFilePath);
            using Stream outputFileStream = File.Create(outputFilePath);
            using PGP pgp = GetPGPInstance();
            await pgp.DecryptStreamAndVerifyAsync(inputFileStream, outputFileStream, publicKeyStream, privateKeyStream, passPhraseValue);
        }

        /// <inheritdoc/>
        public async Task DecryptStreamAndVerifyAsync(Stream inputStream, Stream outputStream, string publicKeyName, string privateKeyName, string passPhrase = "")
        {
            string publicKeyValue = GetPgpKeyValue(publicKeyName);
            string privateKeyValue = GetPgpKeyValue(privateKeyName);
            string passPhraseValue = string.Empty;
            if (!string.IsNullOrEmpty(passPhrase))
            {
                passPhraseValue = GetPgpPassPhrase(passPhrase);
            }

            using MemoryStream publicKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(publicKeyValue));
            using MemoryStream privateKeyStream = _recyclableMemoryStreamManager.GetStream(new UTF8Encoding().GetBytes(privateKeyValue));

            using PGP pgp = GetPGPInstance();
            await pgp.DecryptStreamAndVerifyAsync(inputStream, outputStream, publicKeyStream, privateKeyStream, passPhraseValue);
            if (outputStream != null && outputStream.Length > 0)
            {
                outputStream.Position = 0;
            }
        }

        private static PGP GetPGPInstance()
        {
            return new PGP
            {
                CompressionAlgorithm = CompressionAlgorithmTag.Zip,
                SymmetricKeyAlgorithm = SymmetricKeyAlgorithmTag.Aes256,
                PublicKeyAlgorithm = PublicKeyAlgorithmTag.RsaGeneral,
                HashAlgorithmTag = HashAlgorithmTag.Sha256,

                // Review for this properties.
                FileType = PGPFileType.Binary,
                PgpSignatureType = PgpSignature.DefaultCertification
            };
        }

        private string GetPgpKeyValue(string pgpKeyName)
        {
            // PGP Keys are stored in base64 in key vault
            string encodedPgpKeyValue = _encryptionKeyFinder.FindKeyValue(pgpKeyName);
            byte[] base64PgpKeyValue = Convert.FromBase64String(encodedPgpKeyValue);
            // Decoded value match the PGP key format
            string pgpKeyValue = Encoding.UTF8.GetString(base64PgpKeyValue);
            return pgpKeyValue;
        }

        private string GetPgpPassPhrase(string passPhrase)
        {
            string passPhraseValue = _encryptionKeyFinder.FindKeyValue(passPhrase);
            return passPhraseValue;
        }
    }
}
