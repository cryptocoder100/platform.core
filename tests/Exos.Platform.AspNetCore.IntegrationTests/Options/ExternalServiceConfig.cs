#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.IntegrationTests.Options
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines the <see cref="ExternalServiceConfig" />.
    /// </summary>
    public class ExternalServiceConfig
    {
        /// <summary>
        /// Gets or sets the Host.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the ExternalHost.
        /// </summary>
        public string ExternalHost { get; set; }

        /// <summary>
        /// Gets or sets the Args.
        /// </summary>
        public Dictionary<string, string> Args { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only