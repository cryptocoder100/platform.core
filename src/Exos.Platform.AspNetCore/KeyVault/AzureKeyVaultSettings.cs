namespace Exos.Platform.AspNetCore.KeyVault
{
    using System;

    /// <summary>
    /// Azure Key Vault Settings.
    /// </summary>
    public class AzureKeyVaultSettings
    {
        /// <summary>
        /// Gets or sets the AuthenticationType.
        /// </summary>
        public AzureKeyVaultAuthenticationType AuthenticationType { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the ClientId.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the ClientSecret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the Certificate.
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        /// Gets or sets the Certificate Password..
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the Thumbprint.
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Gets or sets the Key Prefix..
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the Key Vault Url..
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets the time in minutes to wait to reload Azure KeyVault.
        /// </summary>
        public double ReloadInterval { get; set; } = 120;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the delay between retry attempts .
        /// </summary>
        public double RetryDelay { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum permissible delay between retry attempts.
        /// </summary>
        public double MaxDelay { get; set; } = 16;
    }
}
