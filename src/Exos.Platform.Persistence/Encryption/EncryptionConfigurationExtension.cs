namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.IO;
    using Exos.Platform.AspNetCore.KeyVault;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension to configure sql/cosmos/blob encryption.
    /// </summary>
    public static class EncryptionConfigurationExtension
    {
        /// <summary>
        /// Configure encryption services.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEncryptionServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration != null)
            {
                // Initialize Encryption service
                services.AddScoped<IDatabaseEncryption, AesGcmDatabaseEncryption>();
                services.AddScoped<IBlobEncryption, AesCbcBlobEncryption>();
                services.AddScoped<IPgpEncryption, PgpEncryption>();
                services.AddScoped<IDatabaseHashing, DatabaseHashing>();
                services.AddScoped<ISshKeyFinder, SshKeyFinder>();

                if (configuration.GetSection("EncryptionKeyMappingConfiguration").Exists())
                {
                    // Configure mapping between subdomain and key names
                    services.Configure<EncryptionKeyMappingConfiguration>(configuration.GetSection("EncryptionKeyMappingConfiguration"));
                }

                if (configuration.GetSection("EncryptionKeyVault").Exists())
                {
                    // Below settings are to use keyvault to store and find keys
                    if (string.IsNullOrEmpty(configuration.GetValue<string>("EncryptionKeyVault:KeyVaultSecretsPathVariableName")))
                    {
                        // Key Vault properties are stored in appsettings file.
                        services.Configure<EncryptionKeyVaultSettings>(configuration.GetSection("EncryptionKeyVault"));
                    }
                    else
                    {
                        // Key vault properties are stored in text files and in environment variables.
                        string keyVaultFilesPath = ReadValueFromEnvironmentVariable(configuration.GetValue<string>("EncryptionKeyVault:KeyVaultSecretsPathVariableName"));
                        services.Configure<EncryptionKeyVaultSettings>(options =>
                        {
                            options.AuthenticationType = AzureKeyVaultAuthenticationType.Secret;
                            options.TenantId = ReadValueFromConfigFile(keyVaultFilesPath, configuration.GetValue<string>("EncryptionKeyVault:KeyVaultTenantIdFileName"));
                            options.ClientId = ReadValueFromConfigFile(keyVaultFilesPath, configuration.GetValue<string>("EncryptionKeyVault:KeyVaultClientIdFileName"));
                            options.ClientSecret = ReadValueFromConfigFile(keyVaultFilesPath, configuration.GetValue<string>("EncryptionKeyVault:KeyVaultClientSecretFileName"));
                            options.Url = new Uri(ReadValueFromEnvironmentVariable(configuration.GetValue<string>("EncryptionKeyVault:KeyVaultUrlVariableName")));
                            options.ReloadInterval = configuration.GetValue<double>("EncryptionKeyVault:ReloadInterval");
                        });
                    }

                    services.AddScoped<EncryptionKeyVaultSecretClient>();
                    services.AddScoped<ICommonKeyEncryptionFinder, KeyVaultCommonKeyEncryptionFinder>();
                    services.AddScoped<IEncryptionKeyFinder, KeyVaultEncryptionKeyFinder>();
                    services.AddMemoryCache();
                }
                else if (configuration.GetSection("EncryptionKeyConfiguration").Exists())
                {
                    // Below settings are to use appsettings file to store and find keys
                    services.Configure<EncryptionKeyConfiguration>(configuration.GetSection("EncryptionKeyConfiguration"));
                    services.AddScoped<IEncryptionKeyFinder, AppSettingsEncryptionKeyFinder>();
                    services.AddScoped<ICommonKeyEncryptionFinder, AppSettingsCommonKeyEncryptionFinder>();
                }
                else
                {
                    // Add a default implementation, if none of the sections above is found in appsettings file
                    // the following key finder is added and data is not encrypted.
                    services.AddScoped<IEncryptionKeyFinder, AppSettingsEncryptionKeyFinder>();
                    services.AddScoped<ICommonKeyEncryptionFinder, AppSettingsCommonKeyEncryptionFinder>();
                }
            }

            return services;
        }

        private static string ReadValueFromConfigFile(string filePath, string fileName)
        {
            // CWE-73 flaw is showed in the line below, will be mitigated since values are coming from vaultsettings file.
            string fileValue = File.ReadAllText($"{filePath}/{fileName}");
            return fileValue;
        }

        private static string ReadValueFromEnvironmentVariable(string variableName)
        {
            string variableValue = Environment.GetEnvironmentVariable(variableName);
            return variableValue;
        }
    }
}
