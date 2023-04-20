#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
namespace Exos.Platform.Messaging.Core.StreamListener
{
    using Exos.Platform.Messaging.Core.Listener;

    /// <summary>
    /// ExosStream.
    /// </summary>
    public class ExosStream
    {
        /// <summary>
        /// Gets or sets MessageUtfText.
        /// </summary>
        public string MessageUtfText { get; set; }

        /// <summary>
        /// Gets or sets Property.
        /// </summary>
        public MessageProperty Property { get; set; }
    }
}
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix