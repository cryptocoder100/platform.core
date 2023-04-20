namespace Exos.Platform.AspNetCore.KeyVault
{
    using Azure.Security.KeyVault.Secrets;

    /// <inheritdoc/>
    public class AzureKeyVaultSecret : KeyVaultSecret
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultSecret"/> class.
        /// </summary>
        /// <param name="name">The name of the secret.</param>
        /// <param name="value">The value of the secret.</param>
        public AzureKeyVaultSecret(string name, string value) : base(name, value)
        {
        }
    }
}
