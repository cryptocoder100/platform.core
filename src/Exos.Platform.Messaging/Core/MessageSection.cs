#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.Messaging.Core
{
    using System.Collections.Generic;
    using Exos.Platform.Messaging.Core.Listener;

    /// <summary>
    /// Messaging Configuration.
    /// </summary>
    public class MessageSection
    {
        /// <summary>
        /// Gets or sets MessageDb.
        /// </summary>
        public string MessageDb { get; set; }

        /// <summary>
        /// Gets or sets Environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the FailoverConfig.
        /// </summary>
        public FailoverConfig FailoverConfig { get; set; } = new FailoverConfig();

        /// <summary>
        /// Gets or sets the Listener Configurations.
        /// </summary>
        public IList<MessageListenerConfig> Listeners { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only