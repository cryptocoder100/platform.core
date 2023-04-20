namespace Exos.Platform.Persistence.Encryption
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="IPgpEncryption" />.
    /// </summary>
    public interface IPgpEncryption
    {
        /// <summary>
        /// Encrypt file using using a public key.
        /// </summary>
        /// <param name="inputFilePath">Input file name and path<see cref="string"/>.</param>
        /// <param name="outputFilePath">Output file name and path<see cref="string"/>.</param>
        /// <param name="publicKeyName">Public Key Name<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task EncryptFileAsync(string inputFilePath, string outputFilePath, string publicKeyName);

        /// <summary>
        /// Encrypt stream using a public key.
        /// </summary>
        /// <param name="inputStream">Input Stream<see cref="Stream"/>.</param>
        /// <param name="outputStream">Output Stream<see cref="Stream"/>.</param>
        /// <param name="publicKeyName">Public Key Name<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task EncryptStreamAsync(Stream inputStream, Stream outputStream, string publicKeyName);

        /// <summary>
        /// Encrypt file  using a public key and sign using your private key.
        /// </summary>
        /// <param name="inputFilePath">Input file name and path<see cref="string"/>.</param>
        /// <param name="outputFilePath">Output file name and path<see cref="string"/>.</param>
        /// <param name="publicKeyName">Public Key Name<see cref="string"/>.</param>
        /// <param name="privateKeyName">Private Key Name<see cref="string"/>.</param>
        /// <param name="passPhrase">Pass Phrase for the private key.<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task EncryptFileAndSignAsync(string inputFilePath, string outputFilePath, string publicKeyName, string privateKeyName, string passPhrase = "");

        /// <summary>
        /// Encrypt stream using a public key and sign using your private key.
        /// </summary>
        /// <param name="inputStream">Input Stream<see cref="Stream"/>.</param>
        /// <param name="outputStream">Output Stream<see cref="Stream"/>.</param>
        /// <param name="publicKeyName">Public Key Name<see cref="string"/>.</param>
        /// <param name="privateKeyName">Private Key Name<see cref="string"/>.</param>
        /// <param name="passPhrase">Pass Phrase for the private key.<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task EncryptStreamAndSignAsync(Stream inputStream, Stream outputStream, string publicKeyName, string privateKeyName, string passPhrase = "");

        /// <summary>
        /// Decrypt file using the private key and passphrase.
        /// </summary>
        /// <param name="inputFilePath">Input file name and path<see cref="string"/>.</param>
        /// <param name="outputFilePath">Output file name and path<see cref="string"/>.</param>
        /// <param name="privateKeyName">Private Key Name<see cref="string"/>.</param>
        /// <param name="passPhrase">Pass Phrase for the private key.<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task DecryptFileAsync(string inputFilePath, string outputFilePath, string privateKeyName, string passPhrase = "");

        /// <summary>
        /// Decrypt stream using the private key and passphrase.
        /// </summary>
        /// <param name="inputStream">Input Stream<see cref="Stream"/>.</param>
        /// <param name="outputStream">Output Stream<see cref="Stream"/>.</param>
        /// <param name="privateKeyName">Private Key Name<see cref="string"/>.</param>
        /// <param name="passPhrase">Pass Phrase for the private key.<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task DecryptStreamAsync(Stream inputStream, Stream outputStream, string privateKeyName, string passPhrase = "");

        /// <summary>
        /// Decrypt file and verify the signed file.
        /// </summary>
        /// <param name="inputFilePath">Input file name and path<see cref="string"/>.</param>
        /// <param name="outputFilePath">Output file name and path<see cref="string"/>.</param>
        /// <param name="publicKeyName">Public Key Name<see cref="string"/>.</param>
        /// <param name="privateKeyName">Private Key Name<see cref="string"/>.</param>
        /// <param name="passPhrase">Pass Phrase for the private key.<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task DecryptFileAndVerifyAsync(string inputFilePath, string outputFilePath, string publicKeyName, string privateKeyName, string passPhrase = "");

        /// <summary>
        /// Decrypt stream and verify the signed file.
        /// </summary>
        /// <param name="inputStream">Input Stream<see cref="Stream"/>.</param>
        /// <param name="outputStream">Output Stream<see cref="Stream"/>.</param>
        /// <param name="publicKeyName">Public Key Name<see cref="string"/>.</param>
        /// <param name="privateKeyName">Private Key Name<see cref="string"/>.</param>
        /// <param name="passPhrase">Pass Phrase for the private key.<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task DecryptStreamAndVerifyAsync(Stream inputStream, Stream outputStream, string publicKeyName, string privateKeyName, string passPhrase = "");
    }
}
