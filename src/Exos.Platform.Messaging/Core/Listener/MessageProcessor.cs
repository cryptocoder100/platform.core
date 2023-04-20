namespace Exos.Platform.Messaging.Core.Listener
{
    using System.Threading.Tasks;

    /// <summary>
    /// Message Processor Class, listeners should implement this class and the execute method.
    /// </summary>
    public abstract class MessageProcessor
    {
        /// <summary>
        /// Gets or sets MessageSection configuration.
        /// </summary>
        public MessageSection MessageConfigurationSection { get; set; }

        /// <summary>
        /// Process a message.
        /// </summary>
        /// <param name="messageUtfText">Message Text.</param>
        /// <param name="messageProperty">MessageProperty.</param>
        /// <returns>True if message is processed.</returns>
        public abstract Task<bool> Execute(string messageUtfText, MessageProperty messageProperty);
    }
}
