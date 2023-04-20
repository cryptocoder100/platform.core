#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.IntegrationTests.Options
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Defines the <see cref="ExternalServiceOptions" />.
    /// </summary>
    public class ExternalServiceOptions : IOptions<ExternalServiceOptions>
    {
        /// <summary>
        /// Gets or sets the ExternalServices.
        /// </summary>
        public Dictionary<string, ExternalServiceConfig> ExternalServices { get; set; }

        /// <summary>
        /// Gets the Value.
        /// </summary>
        public ExternalServiceOptions Value { get => this; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only