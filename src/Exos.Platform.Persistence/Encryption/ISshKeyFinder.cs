namespace Exos.Platform.Persistence.Encryption
{
    using System.IO;

    /// <summary>
    /// Defines the <see cref="ISshKeyFinder" />.
    /// </summary>
    public interface ISshKeyFinder
    {
        /// <summary>
        /// Get SSH Key.
        /// </summary>
        /// <param name="keyName">The keyName<see cref="string"/>.</param>
        /// <returns>A byte[] representing a ssh key.</returns>
        public byte[] GetSshKeyBytes(string keyName);

        /// <summary>
        /// Get SSH Key.
        /// </summary>
        /// <param name="keyName">The keyName<see cref="string"/>.</param>
        /// <returns>A Stream representing a ssh key.</returns>
        public Stream GetSshKey(string keyName);
    }
}
