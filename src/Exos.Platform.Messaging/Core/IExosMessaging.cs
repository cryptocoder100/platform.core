namespace Exos.Platform.Messaging.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to publish messages to queue or topics.
    /// </summary>
    public interface IExosMessaging
    {
        /// <summary>
        /// This method will validate and initialize the topic or queue if it is not already.
        /// </summary>
        /// <param name="entityName">Topic or Queue name.</param>
        /// <param name="entityOwner"> Micro service or the module which owns the topic/queue.</param>
        /// <returns>True if is queue/topic is valid and initialized.</returns>
        bool ValidateAndInitializeAzureEntities(string entityName, string entityOwner);

        /// <summary>
        /// Generic message publishing incoming messages. Go through the configuration and get group the entities
        /// and send messages based on configuration.
        /// </summary>
        /// <param name="incomingMessages">Messages to publish.</param>
        /// <returns>Return ExosMessagingConstant.SucessCode or ExosMessagingConstant.WriteFailedCode.</returns>
        Task<int> PublishMessage(IList<ExosMessage> incomingMessages);

        /// <summary>
        /// Publish message to Topic.
        /// </summary>
        /// <param name="incomingMessage">Message to publish.</param>
        /// <returns>Return ExosMessagingConstant.SucessCode or ExosMessagingConstant.WriteFailedCode.</returns>
        Task<int> PublishMessageToTopic(ExosMessage incomingMessage);

        /// <summary>
        /// Send group of messages to the topic. if all messages belong to one entity, then leave each message object
        /// configuration property to null. Else populate each message. This way caller does not need to sort the messages.
        /// </summary>
        /// <param name="configuration">Message Configuration.</param>
        /// <param name="incomingMessages">Messages to publish.</param>
        /// <returns>Return ExosMessagingConstant.SucessCode or ExosMessagingConstant.WriteFailedCode.</returns>
        Task<int> PublishMessageToTopic(MessageConfig configuration, IList<ExosMessage> incomingMessages);

        /// <summary>
        /// Publish message to queue multiple messages. Pass the queue name and caller id in first parameter.
        /// </summary>
        /// <param name="configuration">Message Configuration.</param>
        /// <param name="incomingMessages">Messages to publish.</param>
        /// <returns>Return ExosMessagingConstant.SucessCode or ExosMessagingConstant.WriteFailedCode.</returns>
        Task<int> PublishMessageToQueue(MessageConfig configuration, IList<ExosMessage> incomingMessages);

        /// <summary>
        /// Publish message to queue. Pass the queue name and caller id in first parameter.
        /// </summary>
        /// <param name="incomingMessage">Message to publish.</param>
        /// <returns>Return ExosMessagingConstant.SucessCode or ExosMessagingConstant.WriteFailedCode.</returns>
        Task<int> PublishMessageToQueue(ExosMessage incomingMessage);

        /// <summary>
        /// Retrieve messages from the dead letter queue. Messages get removed from the queue after the call.
        /// So the read messages must be kept on client side if needed for later processing.
        /// </summary>
        /// <param name="topicName">Topic name.</param>
        /// <param name="topicOwnerName">Topic owner name.</param>
        /// <param name="subscriptionName">Subscription Name.</param>
        /// <param name="batchCount"> This will maximum return batch size*2 since we are looking at East and West.</param>
        /// Issue https://github.com/Azure/azure-service-bus-dotnet/issues/441
        /// <returns>List of Exos Messages.</returns>
        Task<List<ExosMessage>> ReadDlqMessages(string topicName, string topicOwnerName, string subscriptionName, int batchCount);

        /// <summary>
        /// Publish message to event hub.
        /// </summary>
        /// <param name="incomingMessage">Incoming message <see cref="ExosMessage"/>.</param>
        /// <returns>Task.</returns>
        Task<int> PublishMessageToEventHub(ExosMessage incomingMessage);

        /// <summary>
        /// Generic message publishing incoming messages. Go through the configuration and get group the entities
        /// and send messages based on configuration.
        /// </summary>
        /// <param name="incomingMessages">Messages to publish.</param>
        /// <returns>Return List of Messages not published/failed.</returns>
        Task<IList<ExosMessage>> PublishMessages(IList<ExosMessage> incomingMessages);
    }
}
