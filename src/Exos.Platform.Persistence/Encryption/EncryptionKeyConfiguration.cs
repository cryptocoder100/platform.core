#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.Persistence.Encryption
{
    using System.Collections.Generic;

    /// <summary>
    /// Configurations for <see cref="EncryptionKey"/>.
    /// To store keys in an appsettings.json file.
    /// </summary>
    public class EncryptionKeyConfiguration
    {
        /// <summary>
        /// Gets or sets the list of Client Encryption keys.
        /// </summary>
        public List<EncryptionKey> EncryptionKeys { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only