#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Team Type Model.
    /// </summary>
    public partial class TeamType
    {
        /// <summary>
        /// Gets or sets user List of Team identifier.
        /// </summary>
        public List<string> TeamIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether team primary flag.
        /// </summary>
        public bool Primary { get; set; }

        /// <summary>
        /// Gets or sets team priority.
        /// </summary>
        public long Priority { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only