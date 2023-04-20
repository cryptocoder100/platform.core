namespace Exos.Platform.AspNetCore.Encryption
{
    /// <summary>
    /// Defines the <see cref="EncryptionConstants" />.
    /// </summary>
    public static class EncryptionConstants
    {
        /// <summary>
        /// The name of the http request header used to find the request subdomain.
        /// </summary>
        public const string EncryptionRequestHeader = "X-Client-Tag";
    }
}
