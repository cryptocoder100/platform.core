#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Single Signed On Data.
    /// </summary>
    public interface ISsoData
    {
        /// <summary>
        /// Gets or sets the UserContext.
        /// </summary>
        UserInfo UserContext { get; set; }

        /// <summary>
        /// Gets or sets the CustomData.
        /// </summary>
        List<NameValue> CustomData { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only