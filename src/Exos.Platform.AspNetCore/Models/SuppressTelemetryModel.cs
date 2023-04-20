namespace Exos.Platform.AspNetCore.Models
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Typed model to accept request for ignoring Telemetry.
    /// </summary>
    public class SuppressTelemetryModel
    {
        /// <summary>
        /// Gets or sets the Type to ignore.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the RegexPath to ignore.
        /// </summary>
        public string RegexPath { get; set; }

        /// <summary>
        /// Gets the Regex
        /// Gets  the RegexPath to ignore.
        /// </summary>
        public Regex Regex => !string.IsNullOrEmpty(RegexPath) ? new Regex(Regex.Escape(RegexPath), RegexOptions.IgnoreCase) : null;

        /// <summary>
        /// Gets or sets the ResultCode to ignore.
        /// </summary>
        public string ResultCode { get; set; }
    }
}
