#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.Persistence.Encryption
{
    using System.Collections.Generic;

    /// <summary>
    /// Configurations for <see cref="EncryptionKeyMapping"/>.
    /// To store key in an appsettings.json file.
    /// This configuration will help to find a key in the key store.
    /// Most of the cases the mapping is based in the request subdomain.
    /// </summary>
    public class EncryptionKeyMappingConfiguration
    {
        /// <summary>
        /// Gets or sets the list of Client Encryption keys.
        /// </summary>
        public List<EncryptionKeyMapping> EncryptionKeyMappings { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only