#pragma warning disable CA1055 // URI-like return values should not be strings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Exos.Platform.AspNetCore.Helpers
{
    /// <summary>
    /// Helper methods for working with Application Insights telemetry.
    /// </summary>
    public static partial class TelemetryHelper
    {
        private static readonly JsonSerializerOptions _sqlQuerySpecOptions = new JsonSerializerOptions
        {
            // IgnoreNullValues = true, Obsolete in .net6 replaced with line below.
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Attempts to add additional tags to the activity.
        /// </summary>
        /// <param name="activity">The <see cref="Activity" />.</param>
        /// <param name="tags">A list of tags to add to the activity.</param>
        public static void TryEnrichActivity(Activity activity, params KeyValuePair<string, string>[] tags)
        {
            if (activity == null || tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, LoggerHelper.SanitizeValue(tag.Value));
            }
        }

        /// <summary>
        /// Attempts to add additional properties to the request telemetry.
        /// </summary>
        /// <param name="context">The request <see cref="HttpContext" />.</param>
        /// <param name="properties">A list of properties to add to the telemetry.</param>
        public static void TryEnrichRequestTelemetry(HttpContext context, params KeyValuePair<string, string>[] properties)
        {
            if (context == null || properties == null)
            {
                return;
            }

            var requestTelemetry = context.Features?.Get<RequestTelemetry>();
            if (requestTelemetry != null)
            {
                foreach (var prop in properties)
                {
                    requestTelemetry.Properties[prop.Key] = LoggerHelper.SanitizeValue(prop.Value);
                }
            }
        }

        /// <summary>
        /// A highly optimized helper for redacting a list of keyword values from the parameters collection of a Cosmos SQL query.
        /// </summary>
        /// <param name="sqlQuerySpec">A JSON serialized "SqlQuerySpec".</param>
        /// <param name="keywords">A list of parameter names to redact.</param>
        /// <param name="replacement">The redaction replacement value.</param>
        /// <returns>The redacted JSON serialized "SqlQuerySpec".</returns>
        [Obsolete("This function is not working as expected and requires more investigation.")]
        public static string RedactSqlQuerySpec(string sqlQuerySpec, IList<string> keywords, string replacement)
        {
            if (string.IsNullOrEmpty(sqlQuerySpec) || (keywords == null || keywords.Count == 0))
            {
                return sqlQuerySpec;
            }

            var modified = false;
            var spec = JsonSerializer.Deserialize<SqlQuerySpec>(sqlQuerySpec);
            if (spec.Parameters != null && spec.Parameters.Count > 0)
            {
                foreach (var p in spec.Parameters)
                {
                    if (string.IsNullOrEmpty(p.Name))
                    {
                        continue;
                    }

                    foreach (var keyword in keywords)
                    {
                        // PERF: Vectorized string compare
                        if (MemoryExtensions.Equals(p.Name.AsSpan(1), keyword.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            p.Value = replacement;
                            modified = true;
                        }
                    }
                }
            }

            if (!modified)
            {
                // PERF: Don't create a new string instance
                return sqlQuerySpec;
            }

            sqlQuerySpec = JsonSerializer.Serialize(spec, _sqlQuerySpecOptions);
            return sqlQuerySpec;
        }

        /// <summary>
        /// A highly optimized helper for redacting a list of keyword values form the query string portion of a URL.
        /// </summary>
        /// <param name="absoluteUrl">The absolute URL to redact.</param>
        /// <param name="keywords">A list of query names to redact.</param>
        /// <param name="replacement">The redaction replacement value.</param>
        /// <returns>The redacted URL.</returns>
        /// <exception cref="InvalidOperationException">The <paramref name="absoluteUrl" /> is not absolute.</exception>
        public static Uri RedactUrl(Uri absoluteUrl, IList<string> keywords, string replacement)
        {
            if (absoluteUrl == null || string.IsNullOrEmpty(absoluteUrl.Query) || (keywords == null || keywords.Count == 0))
            {
                return absoluteUrl;
            }

            var query = RedactQueryString(absoluteUrl.Query, keywords, replacement);
            if (query == absoluteUrl.Query)
            {
                // PERF: Don't create new URI instance
                return absoluteUrl;
            }

            // This is why I disagree with the rule to use Uri class instead of a simple string...
            var baseUrl = absoluteUrl.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);
            return new Uri(baseUrl + query);
        }

        /// <summary>
        /// A highly optimized helper for redacting a list of keyword values from the query string portion of a URL.
        /// </summary>
        /// <param name="url">The URL to redact.</param>
        /// <param name="keywords">A list of query names to redact.</param>
        /// <param name="replacement">The redaction replacement value.</param>
        /// <returns>The redacted URL.</returns>
        public static string RedactUrl(string url, IList<string> keywords, string replacement)
        {
            if (string.IsNullOrEmpty(url) || (keywords == null || keywords.Count == 0))
            {
                return url;
            }

            // PERF: Vectorized search
            var questionIndex = MemoryExtensions.IndexOf(url, '?');
            if (questionIndex == -1)
            {
                // PERF: Don't create a new string instance
                return url;
            }

            var sb = new StringBuilder(url, 0, questionIndex, url.Length); // PERF: Max capacity hint
            var queryString = RedactQueryString(new StringSegment(url, questionIndex, url.Length - questionIndex), keywords, replacement);
            sb.Append(queryString.Buffer, queryString.Offset, queryString.Length);

            return sb.ToString();
        }

        /// <summary>
        /// A highly optimized helper for redacting a list of keyword values in a query string.
        /// </summary>
        /// <param name="queryString">The query string to redact.</param>
        /// <param name="keywords">A list of query names to redact.</param>
        /// <param name="replacement">The redaction replacement value.</param>
        /// <returns>The redacted query string.</returns>
        /// <remarks>
        /// Telemetry item processing is a hot path in our performance tests.
        /// So, while this may seem overboard, it is designed to provide the best possible perf.
        /// </remarks>
        public static StringSegment RedactQueryString(StringSegment queryString, IList<string> keywords, string replacement)
        {
            if ((!queryString.HasValue || queryString.Length == 0) || (keywords == null || keywords.Count == 0))
            {
                return queryString;
            }

            StringBuilder sb = null;
            var enumerable = new QueryStringEnumerable(queryString);
            var flushPos = queryString.Offset;

            // Look for matching query names with keywords.
            // NOTE: We don't have to worry about URL decoding because we don't ever look for keywords with special symbols.
            foreach (var pair in enumerable)
            {
                // PERF: Don't process KVP that has no value
                if (pair.EncodedValue.Length == 0)
                {
                    continue;
                }

                foreach (var keyword in keywords)
                {
                    // PERF: Vectorized string compare
                    if (MemoryExtensions.Equals(pair.EncodedName.AsSpan(), keyword.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        // PERF: Delay allocate StringBuilder
                        if (sb == null)
                        {
                            sb = new StringBuilder(queryString.Length); // PERF: Max capacity hint
                        }

                        // Flush everything since the last flush (includes existing query name and equals),
                        // add the replacement, then set flush position to AFTER existing query value.
                        sb.Append(queryString.Buffer, flushPos, pair.EncodedValue.Offset - flushPos);
                        sb.Append(replacement);
                        flushPos = pair.EncodedValue.Offset + pair.EncodedValue.Length;
                        break;
                    }
                }
            }

            if (sb == null)
            {
                // PERF: We didn't redact anything; return the original
                return queryString;
            }

            // Flush any remaining content
            sb.Append(queryString.Buffer, flushPos, queryString.Offset + queryString.Length - flushPos);
            return sb.ToString();
        }

        private class SqlQuerySpec
        {
            [JsonPropertyName("query")]
            public string QueryText { get; set; }

            [JsonPropertyName("parameters")]
            public List<SqlParameter> Parameters { get; set; }
        }

        private class SqlParameter
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }
        }
    }
}
