#pragma warning disable CA1055 // URI-like return values should not be strings

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Exos.Platform.AspNetCore.Helpers
{
    /// <summary>
    /// Helper methods for working with <see cref="IConfiguration" />.
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// Writes the current in-memory configuration to a CSV file for debugging.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration" /> to write.</param>
        /// <param name="filePath">The path to a new CSV file.</param>
        /// <param name="includeExosMacros">Whether to include raw ExosMacro tokens.</param>
        public static void DumpToCsvFile(IConfiguration configuration, string filePath, bool includeExosMacros = false)
        {
            using var fileWriter = new StreamWriter(filePath);
            fileWriter.WriteLine("Key,Value");

            var pairs = configuration
                .AsEnumerable()
                .Where(kvp => includeExosMacros ? true : !kvp.Key.StartsWith("ExosMacro:", StringComparison.OrdinalIgnoreCase))
                .OrderBy(kvp => kvp.Key);

            foreach (var kvp in pairs)
            {
                fileWriter.WriteLine($"{MakeCsvSafe(kvp.Key)},{MakeCsvSafe(kvp.Value)}");
            }

            fileWriter.Flush();
        }

        /// <summary>
        /// Returns the URL specified in the "AZURE_CONFIGURATION_URL" configuration key.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration" /> instance to use.</param>
        /// <returns>The endpoint if found; otherwise, <c>null</c>.</returns>
        public static string GetAppConfigurationUrl(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return configuration["AZURE_CONFIGURATION_URL"];
        }

        /// <summary>
        /// Returns whether the Azure Configuration endpoint is specified in the configuration.
        /// </summary>
        /// <param name="configuration">he <see cref="IConfiguration" /> instance to use.</param>
        /// <returns><c>true</c> if the endpoint is found; otherwise, <c>false</c>.</returns>
        public static bool IsAppConfigurationEnabled(IConfiguration configuration)
        {
            var url = GetAppConfigurationUrl(configuration);
            return !string.IsNullOrEmpty(url);
        }

        private static string MakeCsvSafe(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var mustQuote = str.Contains(',', StringComparison.InvariantCulture)
                || str.Contains('"', StringComparison.InvariantCulture)
                || str.Contains('\r', StringComparison.InvariantCulture)
                || str.Contains('\n', StringComparison.InvariantCulture);

            if (mustQuote)
            {
                var sb = new StringBuilder(str.Length + 5);
                sb.Append('"');
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                    {
                        // Escape quote
                        sb.Append('"');
                    }
                }

                sb.Append('"');

                return sb.ToString();
            }

            return str;
        }
    }
}

// References:
// https://stackoverflow.com/questions/6377454/escaping-tricky-string-to-csv-format