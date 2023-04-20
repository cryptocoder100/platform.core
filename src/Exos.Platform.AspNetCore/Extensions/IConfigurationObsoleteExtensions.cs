#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable SA1201 // Elements should appear in the correct order
namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Defines the <see cref="IConfigurationObsoleteExtensions" />.
    /// </summary>
    [Obsolete("THIS EXTENSION CLASS USES THE OLD WAY OF TOKENIZING AZURE KEY VAULT DURING SERVICE STARTUP.")]
    public static class IConfigurationObsoleteExtensions
    {
        /// <summary>
        /// Gets the specified section and injects any environment variables into the section's tokens.
        /// </summary>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <param name="section">The section<see cref="string"/>.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        public static IConfigurationSection GetTokenizedSection(this IConfiguration configuration, string section)
        {
            IConfigurationSection configurationSection = configuration.GetSection(section);
            Dictionary<string, string> tokenDictionary = new Dictionary<string, string>();

            // Crawl each section and extract the token names
            CrawlSectionForTokenNames(configurationSection, ref tokenDictionary);

            // Crawl each section and extract the token values
            CrawlSectionForTokenValues(configurationSection, ref tokenDictionary);

            // Replace tokens with values
            ReplaceTokenValues(configurationSection, tokenDictionary);

            return configurationSection;
        }

        /// <summary>
        /// Gets a connection string and injects service specific variables (such as DBName) given a connection string type.
        /// </summary>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <param name="type">The type<see cref="ConnectionStringType"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string GetConnectionString(this IConfiguration configuration, ConnectionStringType type)
        {
            IConfigurationSection section = configuration.GetTokenizedSection("SQL");
            foreach (var child in section.GetChildren())
            {
                if (child.Value != null)
                {
                    if (child.Key == "AzureReadWriteConnectionString")
                    {
                        child.Value = child.Value.Replace(@"#{DBName}", configuration.GetValue<string>("SQL:DBName"));
                        child.Value = child.Value.Replace(@"#{ServiceName}", configuration.GetValue<string>("SQL:ServiceName"));
                    }

                    if (child.Key == "IaaSReadWriteConnectionString")
                    {
                        child.Value = child.Value.Replace(@"#{DBName}", configuration.GetValue<string>("SQL:DBName"));
                        child.Value = child.Value.Replace(@"#{ServiceName}", configuration.GetValue<string>("SQL:ServiceName"));
                    }

                    if (child.Key == "MessagingReadWriteConnectionString")
                    {
                        child.Value = child.Value.Replace(@"#{DBName}", configuration.GetValue<string>("SQL:MessagingDBName"));
                        child.Value = child.Value.Replace(@"#{ServiceName}", configuration.GetValue<string>("SQL:ServiceName"));
                    }

                    if (child.Key == "ICTReadWriteConnectionString")
                    {
                        child.Value = child.Value.Replace(@"#{DBName}", configuration.GetValue<string>("SQL:ICTDBName"));
                        child.Value = child.Value.Replace(@"#{ServiceName}", configuration.GetValue<string>("SQL:ServiceName"));
                    }
                }
            }

            string returnConnection = string.Empty;
            switch (type)
            {
                case ConnectionStringType.Azure:
                    returnConnection = configuration.GetValue<string>("SQL:AzureReadWriteConnectionString");
                    break;
                case ConnectionStringType.IaaS:
                    returnConnection = configuration.GetValue<string>("SQL:IaasReadWriteConnectionString");
                    break;
                case ConnectionStringType.Messaging:
                    returnConnection = configuration.GetValue<string>("SQL:MessagingReadWriteConnectionString");
                    break;
                case ConnectionStringType.Ict:
                    returnConnection = configuration.GetValue<string>("SQL:ICTReadWriteConnectionString");
                    break;
                case ConnectionStringType.Redis:
                    returnConnection = configuration.GetValue<string>("Redis:ReadWriteConnectionString");
                    break;
                default:
                    break;
            }

            return returnConnection;
        }

        /// <summary>
        /// Get Tokenized Value.
        /// </summary>
        /// <typeparam name="T">.</typeparam>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/>.</param>
        /// <param name="key">The key<see cref="string"/>.</param>
        /// <returns>The tokenized value.</returns>
        public static T GetTokenizedValue<T>(this IConfiguration configuration, string key)
        {
            int indexLastColon = key.LastIndexOf(':');
            string parentSection = indexLastColon != -1 ? key.Substring(0, indexLastColon) : key;

            IConfigurationSection section = configuration.GetTokenizedSection(parentSection);
            return section.GetValue<T>(key.Substring(indexLastColon + 1));
        }

        private static void ReplaceTokenValues(IConfigurationSection configurationSection, Dictionary<string, string> tokenDictionary)
        {
            foreach (var child in configurationSection.GetChildren())
            {
                if (child.Value != null)
                {
                    foreach (string token in tokenDictionary.Keys)
                    {
                        child.Value = child.Value.Replace(token, tokenDictionary[token]);
                    }
                }
                else
                {
                    ReplaceTokenValues(child, tokenDictionary);
                }
            }
        }

        private static void CrawlSectionForTokenValues(IConfigurationSection configurationSection, ref Dictionary<string, string> tokenDictionary)
        {
            foreach (var child in configurationSection.GetChildren())
            {
                if (child.Value != null && !child.Value.StartsWith("#{", StringComparison.OrdinalIgnoreCase) && tokenDictionary.ContainsKey($"#{{{child.Key}}}"))
                {
                    tokenDictionary[$"#{{{child.Key}}}"] = child.Value;
                }
                else
                {
                    CrawlSectionForTokenValues(child, ref tokenDictionary);
                }
            }
        }

        private static void CrawlSectionForTokenNames(IConfigurationSection configurationSection, ref Dictionary<string, string> tokenDictionary)
        {
            foreach (var child in configurationSection.GetChildren())
            {
                if (child.Value != null)
                {
                    FindTokens(child.Value, ref tokenDictionary);
                }
                else
                {
                    CrawlSectionForTokenNames(child, ref tokenDictionary);
                }
            }
        }

        private static void FindTokens(string connectionString, ref Dictionary<string, string> tokenDictionary)
        {
            Match match = Regex.Match(connectionString, @"(\#){([A-Za-z0-9\-]+)}");
            while (match.Success)
            {
                if (!tokenDictionary.ContainsKey(match.Value))
                {
                    tokenDictionary.Add(match.Value, null);
                }

                match = match.NextMatch();
            }
        }

        /// <summary>
        /// Defines the ConnectionStringType.
        /// </summary>
        public enum ConnectionStringType
        {
            /// <summary>
            /// Defines the Azure.
            /// </summary>
            Azure,

            /// <summary>
            /// Defines the IaaS.
            /// </summary>
            IaaS,

            /// <summary>
            /// Defines the Messaging.
            /// </summary>
            Messaging,

            /// <summary>
            /// Defines the Ict.
            /// </summary>
            Ict,

            /// <summary>
            /// Defines the Redis.
            /// </summary>
            Redis,
        }
    }
}
#pragma warning restore CA1062 // Validate arguments of public methods
#pragma warning restore CA1307 // Specify StringComparison for clarity
#pragma warning restore SA1201 // Elements should appear in the correct order