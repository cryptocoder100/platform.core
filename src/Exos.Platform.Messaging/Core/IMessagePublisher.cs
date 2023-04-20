namespace Exos.Platform.Messaging.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Message Publishing Helper.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Gets or Sets the Message Publishing Environment Configuration.
        /// </summary>
        string Environment { get; set; }

        /// <summary>
        /// Write message to Topic.
        /// </summary>
        /// <param name="messageEntity">MessageEntity.</param>
        /// <param name="brokerMessage">List of Messages.</param>
        /// <returns>Completed Task.</returns>
        Task WriteToTopic(MessageEntity messageEntity, IList<Message> brokerMessage);

        /// <summary>
        /// Write message to Queue.
        /// </summary>
        /// <param name="messageEntity">MessageEntity.</param>
        /// <param name="brokerMessage">List of Messages.</param>
        /// <returns>Completed Task.</returns>
        Task WriteToQueue(MessageEntity messageEntity, IList<Message> brokerMessage);

        /// <summary>
        /// Write message to EventHub.
        /// </summary>
        /// <param name="messageEntity">MessageEntity.</param>
        /// <param name="brokerMessage">List of Messages.</param>
        /// <returns>Completed Task.</returns>
        Task WriteToEventHub(MessageEntity messageEntity, IList<EventData> brokerMessage);
    }
}
