namespace Exos.Platform.Persistence.Encryption
{
    using System;

    /// <summary>
    /// Represent a Encryption Key Mapping.
    /// </summary>
    public class EncryptionKeyMapping
    {
        /// <summary>
        /// Gets or sets the Key Identifier
        /// the request subdomain like api.exostechnology.com.
        /// </summary>
        public string KeyIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the Key Name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the KeyVaultUrl.
        /// </summary>
        public Uri KeyVaultUrl { get; set; }

        /// <summary>
        /// Gets or sets the KeyNameBase.
        /// </summary>
        public string KeyNameBase { get; set; }
    }
}
