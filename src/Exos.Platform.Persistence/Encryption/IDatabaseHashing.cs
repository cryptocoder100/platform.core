namespace Exos.Platform.Persistence.Encryption
{
    /// <summary>
    /// Defines the <see cref="IDatabaseHashing" />.
    /// </summary>
    public interface IDatabaseHashing
    {
        /// <summary>
        /// Generate a hash value.
        /// </summary>
        /// <param name="valueToHash">The value to Hash<see cref="string"/>.</param>
        /// <returns>Hashed value in a hex string.</returns>
        public string HashStringToHex(string valueToHash);

        /// <summary>
        /// Generate a hash value.
        /// </summary>
        /// <param name="valueToHash">The value to Hash<see cref="string"/>.</param>
        /// <returns>Hashed value in a byte[]/>.</returns>
        public byte[] HashStringToByte(string valueToHash);

        /// <summary>
        /// Generate a hash value.
        /// </summary>
        /// <param name="valueToHash">The value to Hash<see cref="string"/>.</param>
        /// <returns>Hashed value in a base64 string.</returns>
        public string HashStringToBase64(string valueToHash);
    }
}
