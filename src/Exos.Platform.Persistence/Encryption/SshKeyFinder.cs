namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.IO;

    /// <summary>
    /// Defines the <see cref="SshKeyFinder" />.
    /// </summary>
    public class SshKeyFinder : ISshKeyFinder
    {
        private readonly ILogger<SshKeyFinder> _logger;
        private readonly IEncryptionKeyFinder _encryptionKeyFinder;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SshKeyFinder"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{SSHKeyFinder}"/>.</param>
        /// <param name="encryptionKeyFinder">The encryptionKeyFinder<see cref="ISshKeyFinder"/>.</param>
        /// <param name="recyclableMemoryStreamManager">The recyclableMemoryStreamManager<see cref="RecyclableMemoryStreamManager"/>.</param>
        public SshKeyFinder(ILogger<SshKeyFinder> logger, IEncryptionKeyFinder encryptionKeyFinder, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _encryptionKeyFinder = encryptionKeyFinder ?? throw new ArgumentNullException(nameof(encryptionKeyFinder));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager ?? throw new ArgumentNullException(nameof(recyclableMemoryStreamManager));
        }

        /// <inheritdoc/>
        public byte[] GetSshKeyBytes(string keyName)
        {
            string sshEncodeKeyValue = _encryptionKeyFinder.FindKeyValue(keyName);
            byte[] pgpKeyValue = Convert.FromBase64String(sshEncodeKeyValue);
            return pgpKeyValue;
        }

        /// <inheritdoc/>
        public Stream GetSshKey(string keyName)
        {
            string sshEncodeKeyValue = _encryptionKeyFinder.FindKeyValue(keyName);
            byte[] pgpKeyValue = Convert.FromBase64String(sshEncodeKeyValue);
            var sshKeyStream = _recyclableMemoryStreamManager.GetStream(pgpKeyValue);
            return sshKeyStream;
        }
    }
}
