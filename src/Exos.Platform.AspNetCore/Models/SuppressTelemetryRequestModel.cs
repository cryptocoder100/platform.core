#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Typed model to accept request for ignoring Telemetry.
    /// </summary>
    public class SuppressTelemetryRequestModel
    {
        /// <summary>
        /// Gets or sets the SuppressTelemetryRequests.
        /// </summary>
        public List<SuppressTelemetryModel> SuppressTelemetryRequests { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only