namespace Exos.Platform.Persistence.Encryption
{
    /// <summary>
    /// Returns the standard common key.
    /// </summary>
    /// <returns>common key.</returns>
    public interface ICommonKeyEncryptionFinder
    {
        /// <summary>
        /// Gets the key name of the common encryption key.
        /// </summary>
        public string CommonEncryptionKeyName { get; }

        /// <summary>
        /// Gets the base name of the Common encryption key.
        /// </summary>
        public string CommonEncryptionKeyBaseName { get; }

        /// <summary>
        /// Gets the key name of the common hashing salt.
        /// </summary>
        public string CommonHashingSaltName { get; }

        /// <summary>
        /// Gets the base name of the Db Hashing Salt.
        /// </summary>
        public string CommonHashingSaltBaseName { get; }

        /// <summary>
        /// Returns the standard hashing salt key.
        /// </summary>
        /// <returns>hashing salt key.</returns>
        public EncryptionKey GetCommonHashingSalt();

        /// <summary>
        /// Returns the standard common key.
        /// </summary>
        /// <returns>common key.</returns>
        public EncryptionKey GetCommonEncryptionKey();
    }
}