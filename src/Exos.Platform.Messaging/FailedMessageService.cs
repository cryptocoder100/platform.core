namespace Exos.Platform.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class FailedMessageService : IFailedMessageService
    {
        private readonly IMessagingRepository _repository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<FailedMessageService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailedMessageService"/> class.
        /// </summary>
        /// <param name="options">MessageSection.</param>
        /// <param name="configuration">configuration.</param>
        /// <param name="loggerFactory">A logger factory for writing trace messages.</param>
        public FailedMessageService(IOptions<MessageSection> options, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<FailedMessageService>();

            _repository = new MessagingRepository(new MessagingDbContext(options.Value.MessageDb), configuration, _loggerFactory.CreateLogger<MessagingRepository>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FailedMessageService"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        public FailedMessageService(IMessagingRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByTransactionIds(string[] transactionIds)
        {
            return _repository.GetFailedMessagesByTransactionIds(transactionIds);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByFailedDate(DateTime failedDayStartTime, DateTime failedDayEndTime)
        {
            return _repository.GetFailedMessagesByFailedDate(failedDayStartTime, failedDayEndTime);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByIds(long[] messageIds)
        {
            return _repository.GetFailedMessagesByIds(messageIds);
        }

        /// <inheritdoc/>
        public IEnumerable<FailedSubscriptionMessageLog> GetAllFailedMessages()
        {
            return _repository.GetAll<FailedSubscriptionMessageLog>();
        }

        /// <inheritdoc/>
        public Task<int> UpdateFailedMessageStatus(long messageId, string status)
        {
            return _repository.UpdateFailedMessageStatus(messageId, status);
        }

        /// <inheritdoc/>
        public Task<int> UpdateFailedMessageErrorMessage(long messageId, string message)
        {
            return _repository.UpdateFailedMessageErrorMessage(messageId, message);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByTopicInDateRange(string topic, DateTime failedStartTime, DateTime failedEndTime)
        {
            return _repository.GetFailedMessagesByTopicInDateRange(topic, failedStartTime, failedEndTime);
        }

        /// <inheritdoc/>
        public Task<int> UpdateFailedMessageIsActive(long[] messageIds, bool isActive)
        {
            return _repository.UpdateFailedMessageIsActive(messageIds, isActive);
        }
    }
}
