#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// AssociatedClient.
    /// </summary>
    public class AssociatedClient
    {
        /// <summary>
        /// Gets or sets MasterClientId.
        /// </summary>
        public long MasterClientId { get; set; }

        /// <summary>
        /// Gets or sets SubClientId.
        /// </summary>
        public long SubClientId { get; set; }
    }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes