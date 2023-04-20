namespace Exos.Platform.AspNetCore.KeyVault
{
    using System;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    /// <inheritdoc/>
    public class AzureKeyVaultFilter : IKeyVaultSecretManager
    {
        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultFilter"/> class.
        /// </summary>
        /// <param name="prefix">Key Prefix.</param>
        public AzureKeyVaultFilter(string prefix)
        {
            _prefix = string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix + "-";
        }

        /// <inheritdoc/>
        public bool Load(SecretItem secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            return secret.Identifier.Name.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public string GetKey(SecretBundle secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            return secret.SecretIdentifier.Name.Substring(_prefix.Length);
        }
    }
}
