namespace Exos.Platform.Messaging.Core.StreamListener
{
    using System;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.EventHubs.Processor;

    /// <summary>
    /// EventConsumerFactory.
    /// </summary>
    public class EventConsumerFactory : IEventProcessorFactory
    {
        private IServiceProvider _serviceProvider;
        private ExosStreamProcessor _exosStreamProcessor;
        private MessageEntity _messageEntity;
        private IMessagingRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventConsumerFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">serviceProvider.</param>
        /// <param name="exosStreamProcessor">exosStreamProcessor.</param>
        /// <param name="repository">repository.</param>
        /// <param name="messageEntity">messageEntity.</param>
        public EventConsumerFactory(IServiceProvider serviceProvider, ExosStreamProcessor exosStreamProcessor, IMessagingRepository repository, MessageEntity messageEntity)
        {
            _serviceProvider = serviceProvider;
            _exosStreamProcessor = exosStreamProcessor;
            _messageEntity = messageEntity;
            _repository = repository;
        }

        /// <summary>
        /// CreateEventProcessor.
        /// </summary>
        /// <param name="context">context.</param>
        /// <returns>event processor.</returns>
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            var consumer = new ExosStreamConsumer(_serviceProvider);
            consumer.ExosStreamProcessor = _exosStreamProcessor;
            consumer.MessageEntity = _messageEntity;
            consumer.MessagingRepository = _repository;
            return consumer;
        }
    }
}
