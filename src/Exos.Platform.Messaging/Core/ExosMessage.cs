namespace Exos.Platform.Messaging.Core
{
    /// <summary>
    /// This model is used as the data model for Exos message.
    /// </summary>
    public class ExosMessage
    {
        /// <summary>
        /// Gets or sets Configuration data for publishing.
        /// </summary>
        public MessageConfig Configuration { get; set; }

        /// <summary>
        /// Gets or sets Message Data contains payload and message related information.
        /// </summary>
        public MessageData MessageData { get; set; }
    }
}
