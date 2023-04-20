namespace Exos.Platform.Persistence.Encryption
{
    using Exos.Platform.AspNetCore.KeyVault;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Defines the <see cref="EncryptionKeyVaultSecretClient" />.
    /// </summary>
    public class EncryptionKeyVaultSecretClient : AzureKeyVaultSecretClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionKeyVaultSecretClient"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger{EncryptionKeyVaultSecretClient}"/>.</param>
        /// <param name="encryptionKeyVaultSettings"><see cref="IOptions{EncryptionKeyVaultSettings}"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        /// <param name="memoryCache"><see cref="IMemoryCache"/>.</param>
        public EncryptionKeyVaultSecretClient(
            ILogger<EncryptionKeyVaultSecretClient> logger,
            IOptions<EncryptionKeyVaultSettings> encryptionKeyVaultSettings,
            IConfiguration configuration,
            IMemoryCache memoryCache) : base(logger, encryptionKeyVaultSettings, configuration, memoryCache)
        {
        }
    }
}
