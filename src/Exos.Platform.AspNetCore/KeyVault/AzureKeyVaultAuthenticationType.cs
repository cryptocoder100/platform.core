namespace Exos.Platform.AspNetCore.KeyVault
{
    /// <summary>
    /// Azure Key Vault Authentication Type.
    /// </summary>
    public enum AzureKeyVaultAuthenticationType
    {
        /// <summary>
        /// Secret AuthenticationType.
        /// </summary>
        Secret,

        /// <summary>
        ///  Certificate AuthenticationType.
        /// </summary>
        Certificate,

        /// <summary>
        /// Thumbprint AuthenticationType.
        /// </summary>
        Thumbprint,

        /// <summary>
        /// MSI AuthenticationType.
        /// </summary>
        MSI,
    }
}
