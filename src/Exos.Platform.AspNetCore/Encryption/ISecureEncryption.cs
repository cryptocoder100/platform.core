namespace Exos.Platform.AspNetCore.Encryption
{
    /// <summary>
    /// Implementations of symmetric algorithms.
    /// </summary>
    public interface ISecureEncryption
    {
        /// <summary>
        /// Encrypt string.
        /// </summary>
        /// <param name="plainText">String to encrypt.</param>
        /// <param name="keyName">Key name.</param>
        /// <returns>Encrypted String.</returns>
        string EncryptPiiData(string plainText, string keyName);

        /// <summary>
        /// Decrypt string.
        /// </summary>
        /// <param name="encryptedText">Encrypted String.</param>
        /// <returns>Decrypted string.</returns>
        string DecriptPiiData(string encryptedText);
    }
}