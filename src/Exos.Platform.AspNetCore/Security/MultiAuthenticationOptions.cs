#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.Security
{
    using System.Collections.Generic;

    /// <summary>
    /// Multi Authentication Middleware Configuration Options.
    /// </summary>
    public class MultiAuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the Schemes
        /// Gets or sets Authentication Schemes..
        /// </summary>
        public List<string> Schemes { get; set; } = new List<string>();
    }
}
#pragma warning restore CA2227 // Collection properties should be read only