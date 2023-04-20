namespace Exos.Platform.Persistence.Encryption
{
    /// <summary>
    /// Implements how to find the encryption keys.
    /// </summary>
    public interface IEncryptionKeyFinder
    {
        /// <summary>
        /// Find the key value for the given key name.
        /// </summary>
        /// <param name="keyName">Key Name.</param>
        /// <param name="keyVersion">Key Version, if null use default version.</param>
        /// <returns>The key value.</returns>
        string FindKeyValue(string keyName, string keyVersion = null);

        /// <summary>
        /// Find the key name base value for the given key name.
        /// </summary>
        /// <param name="keyName">Key Name.</param>
        /// <param name="keyVersion">Key Version, if null use default version.</param>
        /// <returns>The key name base value.</returns>
        public string FindKeyNameBase(string keyName, string keyVersion = null);

        /// <summary>
        /// Get the current encryption key.
        /// </summary>
        /// <param name="keyIdentifier">Key identifier, to identify the key  in the EncryptionKeyMappingConfiguration.
        /// If is null, request subdomain will be used to find the key in the EncryptionKeyMappingConfiguration.
        /// </param>
        /// <returns>Client Encryption Key.</returns>
        EncryptionKey GetCurrentEncryptionKey(string keyIdentifier = null);

        /// <summary>
        /// Gets the specified key details; only if it is a common key allowed for fail-over decryption.
        /// </summary>
        /// <returns>FailOver Encryption Key.</returns>
        EncryptionKey GetCommonEncryptionKey();

        /// <summary>
        /// Get the encryption key.
        /// </summary>
        /// <param name="keyName">Key Name.</param>
        /// <param name="keyVersion">Key Version, if null use default version.</param>
        /// <returns>Client Encryption Key.</returns>
        EncryptionKey GetEncryptionKey(string keyName, string keyVersion = null);
    }
}
