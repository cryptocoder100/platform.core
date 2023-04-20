#pragma warning disable CA1054 // URI-like parameters should not be strings
#pragma warning disable CA1055 // URI-like return values should not be strings

using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;

namespace Exos.Platform.Helpers
{
    /// <summary>
    /// Helper methods for working with URLs.
    /// </summary>
    public static class UrlHelper
    {
        private const string _replacement = "REDACTED";
        private static readonly Regex _queryRegex = new Regex("([^&=]+)(?:=([^&]*))?", RegexOptions.Compiled);
        private static string[] _keywords = new string[]
        {
            "access_token",
            "auth",
            "authorization",
            "client_assertion",
            "code",
            "emailtoken",
            "id_token",
            "nonce",
            "oauth_token",
            "password",
            "refresh_token",
            "ssn",
            "token",
            "username"
        };

        /// <summary>
        /// Redacts sensitive values from the query string portion of a URL.
        /// </summary>
        /// <param name="url">The absolute URL to redact.</param>
        /// <returns>The redacted URL.</returns>
        public static string RedactUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            var queryIndex = url.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex < 0)
            {
                // No query string
                return url;
            }

            queryIndex++;
            return _queryRegex.Replace(
                url,
                m =>
                {
                    var name = m.Groups[1].Value;
                    if (_keywords.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        return $"{name}={_replacement}";
                    }

                    return m.Value;
                },
                url.Length - queryIndex,
                queryIndex);
        }

        /// <summary>
        /// Sanitize Url data, to prevent SSRF.
        /// </summary>
        /// <param name="uri">Pass Uri object with URL.</param>
        /// <returns>Sanitized URL.</returns>
        public static Uri SanitizeLink(Uri uri)
        {
            var requestUri = uri;
            if (uri != null && !Uri.IsWellFormedUriString(uri.OriginalString, UriKind.RelativeOrAbsolute))
            {
                requestUri = new Uri(HeaderUtilities.RemoveQuotes(uri.OriginalString).Value);
            }

            return requestUri;
        }
    }
}
