namespace Exos.Platform.Messaging.Core.StreamListener
{
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;

    /// <summary>
    /// IExosStreamConsumer.
    /// </summary>
    public interface IExosStreamConsumer
    {
        /// <summary>
        /// Gets or sets ExosStreamProcessor.
        /// </summary>
        ExosStreamProcessor ExosStreamProcessor { get; set; }

        /// <summary>
        /// Gets or sets MessageEntity.
        /// </summary>
        MessageEntity MessageEntity { get; set; }

        /// <summary>
        /// Gets or setsMessagingRepository.
        /// </summary>
        IMessagingRepository MessagingRepository { get; set; }
    }
}
