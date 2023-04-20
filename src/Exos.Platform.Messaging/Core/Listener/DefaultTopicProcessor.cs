#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Core.Listener
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc/>
    public class DefaultTopicProcessor : MessageProcessor
    {
        private readonly IServiceProvider _iServiceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<DefaultTopicProcessor> _logger;
        private IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTopicProcessor"/> class.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider.</param>
        public DefaultTopicProcessor(IServiceProvider serviceProvider)
        {
            _iServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _logger = serviceProvider.GetService<ILogger<DefaultTopicProcessor>>();
            _configuration = null;
        }

        /// <inheritdoc/>
        public override Task<bool> Execute(string messageUtfText, MessageProperty messageProperty)
        {
            if (messageProperty == null)
            {
                throw new ArgumentNullException(nameof(messageProperty));
            }

            try
            {
                // Logic to process
                // _logger.LogDebug($"Processing message {LoggerHelper.SanitizeValue(messageUtfText)}");
                var connectionString = MessageConfigurationSection.MessageDb;
                var databaseContext = new MessagingDbContext(connectionString);
                var repository = new MessagingRepository(databaseContext, _configuration, _loggerFactory.CreateLogger<MessagingRepository>());
                AzureMessageData azureMessageData = JsonSerializer.Deserialize<AzureMessageData>(messageUtfText);
                var messageLog = new MessageLog
                {
                    MessageGuid = Guid.Parse(messageProperty.MessageId),
                    TransactionId = messageProperty.CorrelationId ?? Guid.Empty.ToString(),
                    Publisher = messageProperty.Label,
                    Payload = messageUtfText,
                    MetaData = messageProperty.MetaDataProperties,
                    ServiceBusEntityName = azureMessageData?.TopicName,
                    ReceivedDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                };
                repository.Add(messageLog);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error processing subscription Processing Message Id:{LoggerHelper.SanitizeValue(messageProperty.MessageId)} ", e);
            }

            return Task.FromResult(true);
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
