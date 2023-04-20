#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
namespace Exos.Platform.Messaging.Core.StreamListener
{
    using System;

    /// <summary>
    /// ExosFailedStream.
    /// </summary>
    public class ExosFailedStream
    {
        /// <summary>
        /// Gets or sets ExosStream.
        /// </summary>
        public ExosStream ExosStream { get; set; }

        /// <summary>
        /// Gets or sets Exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix