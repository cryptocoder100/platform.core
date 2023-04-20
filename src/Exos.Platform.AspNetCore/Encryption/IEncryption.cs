namespace Exos.Platform.AspNetCore.Encryption
{
    /// <summary>
    /// Implementation of Data Encryption / Decryption.
    /// </summary>
    public interface IEncryption
    {
        /// <summary>
        /// Encrypt string.
        /// </summary>
        /// <param name="stringToEncrypt">String to encrypt.</param>
        /// <returns>Encrypted string.</returns>
        string Encrypt(string stringToEncrypt);

        /// <summary>
        /// Decrypt String.
        /// </summary>
        /// <param name="encryptedString">Encrypted String.</param>
        /// <returns>Plain Text string.</returns>
        string Decrypt(string encryptedString);
    }
}
