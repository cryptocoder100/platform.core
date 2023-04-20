#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.AspNetCore.Middleware
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for MaskTelemetryProcessor class.
    /// </summary>
    public class MaskTelemetryProcessorOptions
    {
        /// <summary>
        /// Gets or sets the values to mask in Telemetry events.
        /// </summary>
        public List<string> MaskTelemetryValues { get; set; } = new List<string>();
    }
}
#pragma warning restore CA2227 // Collection properties should be read only