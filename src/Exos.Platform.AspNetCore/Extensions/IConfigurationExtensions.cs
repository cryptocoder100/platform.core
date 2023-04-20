namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Managed extension methods for IConfiguration-implemented objects.
    /// </summary>
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// Replaces any token (i.e. ${tokenName} ) in the supplied string and returns the resolved result.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="input">The string being parsed.</param>
        /// <param name="errorBuilder">The error builder.</param>
        /// <returns>String with tokens replaced.</returns>
        /// <remarks>
        /// For this to work, the appconfig.json must contain the key-value pair.
        /// <add key="MacroName" value="${MacroName}" />
        /// </remarks>
        public static string ReplaceTokens(this IConfiguration configuration, string input, StringBuilder errorBuilder = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            if (errorBuilder == null)
            {
                errorBuilder = new StringBuilder();
            }

            var matches = Regex.Matches(input, @"[\$\#]{(.*?)}");
            foreach (Match match in matches)
            {
                var macroKey = match.Groups[1].Value;
                var resolvedValue = configuration.GetValue<string>($"{macroKey}");
                if (resolvedValue == null)
                {
                    errorBuilder.AppendLine(FormattableString.Invariant($"Unknown token referenced: {macroKey}"));
                    Console.WriteLine($"Unknown token referenced: {macroKey}");
                    continue;
                }

                // Check if it's a key vault value that was incorrectly imported.
                if (resolvedValue.StartsWith("{\"uri", StringComparison.InvariantCultureIgnoreCase))
                {
                    errorBuilder.AppendLine(FormattableString.Invariant($"Invalid KeyVault token referenced: {macroKey}"));
                    Console.WriteLine($"Invalid KeyVault token referenced: {macroKey}");
                    continue;
                }

                input = input.Replace(match.Value, resolvedValue, StringComparison.InvariantCultureIgnoreCase);
            }

            return input;
        }
    }
}