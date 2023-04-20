#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA1031 // Do not catch general exception types

namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    /// <summary>
    /// Configuration Builder Extensions.
    /// </summary>
    public static class IConfigurationBuilderExtensions
    {
        /// <summary>
        /// Add external json files to the configuration sources.  The files to add are be found in the
        /// "ExternalJsonFile" configuration section unless otherwise specified.
        /// </summary>
        /// <param name="builder">IConfigurationBuilder Application configuration.</param>
        /// <param name="path">Alternate path to a configuration section.</param>
        /// <returns>Application configuration.</returns>
        public static IConfigurationBuilder AddExternalJsonFiles(this IConfigurationBuilder builder, string path = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var section = builder.Build().GetSection(path ?? "ExternalJsonFiles");
            if (section.Exists())
            {
                var externalFiles = section.GetChildren();
                if (externalFiles.Any())
                {
                    // Determine the insertion point for the external files
                    var lastFileSource = -1;
                    for (var i = 0; i < builder.Sources.Count; i++)
                    {
                        if (builder.Sources[i] is Microsoft.Extensions.Configuration.FileConfigurationSource)
                        {
                            lastFileSource = i;
                        }
                    }

                    // Save and remove the sources after the last file source
                    var savedSources = new List<IConfigurationSource>();
                    if (lastFileSource >= 0)
                    {
                        while (lastFileSource + 1 < builder.Sources.Count)
                        {
                            savedSources.Add(builder.Sources[lastFileSource + 1]);
                            builder.Sources.RemoveAt(lastFileSource + 1);
                        }
                    }

                    // Append the external files onto the shortened configuration sources
                    foreach (var externalFile in externalFiles)
                    {
                        builder.AddJsonFile(externalFile.Value, true, true);
                    }

                    // Restore the saved sources
                    foreach (var savedSource in savedSources)
                    {
                        builder.Sources.Add(savedSource);
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Add the azure key vault to the configuration sources.  The configuration settings are found in the
        /// "AzureKeyVault" section unless otherwise specified.
        /// </summary>
        /// <param name="builder">IConfigurationBuilder Application configuration.</param>
        /// <returns>Application configuration.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            try
            {
                var config = builder.Build();

                // If below fields are not available in appsettings , then we will not bind to the key vault.
                var authType = config["ExosKeyVault:AuthenticationType"];
                var vaultUrl = config["ExosKeyVault:Url"];
                var clientId = config["ExosKeyVault:ClientId"];
                if (string.IsNullOrEmpty(authType) || string.IsNullOrEmpty(vaultUrl) || string.IsNullOrEmpty(clientId))
                {
                    return builder;
                }

                double reloadInterval = Convert.ToDouble(config["ExosKeyVault:ReloadInterval"], CultureInfo.InvariantCulture);

                switch (authType.ToLowerInvariant())
                {
                    case "secret":
                        var clientSecret = config["ExosKeyVault:ClientSecret"];
                        if (string.IsNullOrEmpty(clientSecret))
                        {
                            Console.WriteLine($"Can't Connected to Key Vault:{vaultUrl} using secret authentication type, secret not configured in settings file.");
                            return builder;
                        }

                        AzureKeyVaultConfigurationOptions azureKeyVaultSecretConfigurationOptions = new AzureKeyVaultConfigurationOptions(vaultUrl, clientId, clientSecret)
                        {
                            ReloadInterval = reloadInterval > 0 ? TimeSpan.FromMinutes(reloadInterval) : TimeSpan.FromMinutes(60),
                        };
                        builder.AddAzureKeyVault(azureKeyVaultSecretConfigurationOptions);
                        Console.WriteLine($"Connected to Key Vault:{vaultUrl} using secret, refreshing every {azureKeyVaultSecretConfigurationOptions.ReloadInterval.GetValueOrDefault().Minutes} minutes.");
                        break;

                    case "certificate":
                        // get the certificate and password
                        string certificate = config["ExosKeyVault:Certificate"];
                        string password = config["ExosKeyVault:Password"];
                        if (string.IsNullOrEmpty(certificate) || string.IsNullOrEmpty(password))
                        {
                            Console.WriteLine($"Can't Connected to Key Vault:{vaultUrl} using certificate authentication type, certificate/password not configured in settings file.");
                            return builder;
                        }

                        X509Certificate2 x509Certificate = new X509Certificate2(certificate, password);
                        AzureKeyVaultConfigurationOptions azureKeyVaultCertConfigurationOptions = new AzureKeyVaultConfigurationOptions(vaultUrl, clientId, x509Certificate)
                        {
                            ReloadInterval = reloadInterval > 0 ? TimeSpan.FromMinutes(reloadInterval) : TimeSpan.FromMinutes(60),
                        };
                        builder.AddAzureKeyVault(azureKeyVaultCertConfigurationOptions);
                        Console.WriteLine($"Connected to Key Vault:{vaultUrl} using certificate, refreshing every {azureKeyVaultCertConfigurationOptions.ReloadInterval.GetValueOrDefault().Minutes} minutes.");
                        break;

                    case "thumbprint":
                        string thumbPrint = config["ExosKeyVault:Thumbprint"];
                        if (string.IsNullOrEmpty(thumbPrint))
                        {
                            Console.WriteLine($"Can't Connected to Key Vault:{vaultUrl} using thumbprint authentication type, Thumbprint not configured in settings file.");
                            return builder;
                        }

                        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                        {
                            store.Open(OpenFlags.ReadOnly);
                            var storeCerts = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, false);
                            var storeCert = storeCerts.OfType<X509Certificate2>().Single();

                            AzureKeyVaultConfigurationOptions azureKeyVaultThumbConfigurationOptions = new AzureKeyVaultConfigurationOptions(vaultUrl, clientId, storeCert)
                            {
                                ReloadInterval = reloadInterval > 0 ? TimeSpan.FromMinutes(reloadInterval) : TimeSpan.FromMinutes(60),
                            };
                            builder.AddAzureKeyVault(azureKeyVaultThumbConfigurationOptions);
                            Console.WriteLine($"Connected to Key Vault:{vaultUrl} using thumbprint, refreshing every {azureKeyVaultThumbConfigurationOptions.ReloadInterval.GetValueOrDefault().Minutes} minutes.");
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                // We don't have a logger here so we will just write to the console to notify that AKV is not configured.
                Console.WriteLine(ex.Message);
            }

            return builder;
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore CA1031 // Do not catch general exception types