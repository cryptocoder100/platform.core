namespace Exos.Platform.AspNetCore.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Options for the <see cref="IgnoreTelemetryProcessor" />.
    /// </summary>
    public class IgnoreTelemetryProcessorOptions
    {
        /// <summary>
        /// Gets the list of paths to ignore.
        /// </summary>
        public IList<string> IgnorePaths { get; } = new List<string>
        {
            "/api/v1/ping",
            "/api/v1/warmup",
            "/health/ready",
            "/health/live",
            "/favicon.ico",
            "/swagger"
        };
    }
}
