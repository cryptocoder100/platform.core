using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Exos.Platform.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Exos.Platform.AspNetCore.AppConfiguration
{
    internal sealed class ExosAzureConfigurationResolver
    {
        private readonly Regex _macroRegex = new Regex("[$#]{([^}]+)}", RegexOptions.Compiled);
        private readonly IDictionary<string, string> _secrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, SecretClient> _clients = new Dictionary<string, SecretClient>(StringComparer.OrdinalIgnoreCase);

        private readonly SecretClientOptions _clientOptions;
        private readonly IConfiguration _configuration;
        private readonly TokenCredential _credentials;
        private readonly StringBuilder _errorBuilder;

        public ExosAzureConfigurationResolver(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _configuration = configuration;
            _credentials = ExosCredentials.GetDefaultCredential();

            _clientOptions = new SecretClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 5,
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                }
            };

            _errorBuilder = new StringBuilder();
        }

        public string ProcessTokens()
        {
            // Remember that the configuration will include both: unresolved appsettings.json data and Azure App Configuration
            // "ExosMacro:" data. The task here is to find placeholders and replace them with the equivalent "ExosMacro:" token
            // value. In that process we also determine if it is an AKV URL, and if so, resolve the secret.

            var keys = _configuration.AsEnumerable().Select(kvp => kvp.Key).Where(k => !string.IsNullOrEmpty(k)).OrderBy(k => k);
            foreach (var key in keys)
            {
                // Don't process "ExosMacros:" because that would be recursive.
                // Don't process "FeatureManagement" because those are refreshed dynamically.
                if (key.StartsWith("ExosMacro:", StringComparison.OrdinalIgnoreCase) || key.StartsWith("FeatureManagement", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = _configuration[key];
                if (!string.IsNullOrEmpty(value))
                {
                    _configuration[key] = _macroRegex.Replace(value, ResolveValue);
                }
            }

            return _errorBuilder.ToString();
        }

        private string ResolveValue(Match match)
        {
            var macro = match.Groups[1];
            var value = _configuration[$"ExosMacro:{macro}"];

            // Empty string is acceptable
            if (value != null)
            {
                // Is this an AKV URL?
                if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                {
                    return ResolveSecret(uri, value);
                }

                // Not an AKV URL
                return value;
            }

            // Could not resolve
            _errorBuilder.AppendLine(FormattableString.Invariant($"Unknown token referenced: {macro}"));
            Console.WriteLine(FormattableString.Invariant($"Unknown token referenced: {macro}"));

            return match.Value;
        }

        private string ResolveSecret(Uri uri, string url)
        {
            // Skip URLs that are not AKV secrets
            if (uri.Host.EndsWith("vault.azure.net", StringComparison.OrdinalIgnoreCase) == false)
            {
                return url;
            }
            else if (string.Equals("secrets", uri.Segments?.ElementAtOrDefault(1)?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase) == false)
            {
                return url;
            }

            // The same secret could be used more than once.
            // Do we have a resolved copy?
            if (_secrets.TryGetValue(url, out var secret) == false)
            {
                // Do we have a client?
                if (_clients.TryGetValue(uri.Host, out var client) == false)
                {
                    client = new SecretClient(new Uri(uri.GetLeftPart(UriPartial.Authority)), _credentials, _clientOptions);
                    _clients[uri.Host] = client;
                }

                var secretName = uri.Segments?.ElementAtOrDefault(2)?.TrimEnd('/');
                var secretVersion = uri.Segments?.ElementAtOrDefault(3)?.TrimEnd('/');

                secret = client.GetSecret(secretName, secretVersion)?.Value?.Value;

                _secrets[url] = secret;
            }

            return secret;
        }
    }
}
