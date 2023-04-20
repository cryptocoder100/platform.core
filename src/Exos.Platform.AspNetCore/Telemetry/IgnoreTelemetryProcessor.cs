namespace Exos.Platform.AspNetCore.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Discards request telemetry for the specified URL prefixes.
    /// </summary>
    public class IgnoreTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private readonly IList<string> _ignorePaths;
        private readonly Regex _pathRegex;
        private readonly Regex _operationRegex;
        private readonly bool _enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreTelemetryProcessor" /> class.
        /// </summary>
        /// <param name="next">The next <see cref="ITelemetryProcessor" /> in the chain.</param>
        /// <param name="options">The <see cref="IgnoreTelemetryProcessorOptions" />.</param>
        public IgnoreTelemetryProcessor(ITelemetryProcessor next, IOptions<IgnoreTelemetryProcessorOptions> options)
        {
            _next = next; // May be null
            _ = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _ignorePaths = options.Value.IgnorePaths;
            if (_ignorePaths != null && _ignorePaths.Count > 0)
            {
                _enabled = true;

                // Build a regex for paths starting with...
                var alternatives = string.Join('|', _ignorePaths.Select(s => Regex.Escape(s)));
                _pathRegex = new Regex($"^({alternatives})", RegexOptions.IgnoreCase);
                _operationRegex = new Regex($"^\\w+\\s({alternatives})", RegexOptions.IgnoreCase);
            }
        }

        /// <inheritdoc />
        public void Process(ITelemetry item)
        {
            if (!_enabled)
            {
                _next?.Process(item);
                return;
            }

            // Scan each telemetry type for places paths are used
            if (item is RequestTelemetry requestTelemetry && requestTelemetry.Success == true)
            {
                var path = requestTelemetry.Url?.PathAndQuery;
                if (!string.IsNullOrEmpty(path) && _pathRegex.IsMatch(path))
                {
                    // Ignore
                    return;
                }
            }
            else if (item is DependencyTelemetry dependencyTelemetry && dependencyTelemetry.Success == true)
            {
                var operation = dependencyTelemetry.Context?.Operation?.Name;
                if (!string.IsNullOrEmpty(operation) && _operationRegex.IsMatch(operation))
                {
                    // Ignore
                    return;
                }

                if (Uri.TryCreate(dependencyTelemetry.Data, UriKind.Absolute, out var url))
                {
                    var path = url.PathAndQuery;
                    if (!string.IsNullOrEmpty(path) && _pathRegex.IsMatch(path))
                    {
                        // Ignore
                        return;
                    }
                }
            }
            else if (item is TraceTelemetry traceTelemetry && item is ISupportProperties sp)
            {
                if (sp.Properties.TryGetValue("RequestPath", out var path) && _pathRegex.IsMatch(path))
                {
                    // Ignore
                    return;
                }
            }

            _next?.Process(item);
        }
    }
}
