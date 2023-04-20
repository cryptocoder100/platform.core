namespace Exos.Platform.AspNetCore.Helpers
{
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// Helper methods for working with <see cref="ILogger" /> objects.
    /// </summary>
    public static class LoggerHelper
    {
        /// <summary>
        /// Defines the _options.
        /// </summary>
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            // This is not as scary as it sounds. The default behavior is to escape way more than
            // is necessary by the JSON spec. This options makes it behave more in line with the spec.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Returns a sanitized/escaped string representation of the value suitable for log messages.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value to sanitize.</param>
        /// <returns>The sanitized string if the value is not null; otherwise, <c>null</c>.</returns>
        public static string SanitizeValue<TValue>(TValue value)
        {
            if (value is null)
            {
                // Let Application Insights give us a null representation
                return null;
            }

            var str = JsonSerializer.Serialize(value, _options);
            str = HeaderUtilities.RemoveQuotes(str).Value;

            return str;
        }
    }
}
