#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Repository
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapper;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class MessagingRepository : IMessagingRepository
    {
        private static ConcurrentDictionary<MessageEntityKey, MessageEntity> _cachedMessageEntity = new ConcurrentDictionary<MessageEntityKey, MessageEntity>();
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger _logger;
        private IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingRepository"/> class.
        /// </summary>
        /// <param name="options">Messaging Configuration options.</param>
        /// <param name="configuration">configuration.</param>
        /// <param name="logger">A logger for writing trace messages.</param>
        public MessagingRepository(IOptions<MessageSection> options, IConfiguration configuration, ILogger<MessagingRepository> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _connectionFactory = new MessagingDbContext(options.Value.MessageDb);
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">IDbConnectionFactory.</param>
        /// <param name="configuration">configuration.</param>
        /// <param name="logger">A logger for writing trace messages.</param>
        public MessagingRepository(IDbConnectionFactory connectionFactory, IConfiguration configuration, ILogger<MessagingRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public IDbConnection Connection => _connectionFactory.GetConnection();

        /// <inheritdoc/>
        public bool Add(MessageLog messageLog)
        {
            using (var databaseConnection = Connection)
            {
                try
                {
                    const string insertQuery = "INSERT INTO [msg].[MessageLog] (MessageGuid, Payload, TransactionId, " +
                                               "MetaData, Publisher, ReceivedDateTime, CreatedById, CreatedDate, " +
                                               "LastUpdatedById, LastUpdatedDate,ServiceBusEntityName)" +
                                               " VALUES(@MessageGuid, @Payload, @TransactionId, @MetaData, @Publisher, @ReceivedDateTime," +
                                               "@CreatedById, @CreatedDate, @LastUpdatedById, @LastUpdatedDate,@ServiceBusEntityName)";
                    databaseConnection.Open();
                    databaseConnection.Execute(insertQuery, messageLog);
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error inserting to MessageLog.");
                    return false;
                }
                finally
                {
                    databaseConnection.Close();
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Add(PublishErrorMessageLog publishMessageLog)
        {
            using (var databaseConnection = Connection)
            {
                try
                {
                    const string insertQuery = "INSERT INTO [msg].[PublishErrorMessageLog] (MessageGuid, Payload, TransactionId, " +
                                               " Publisher, MetaData, status,MessageEntityName, FailedDateTime,RetryCount,Comments," +
                                               " CreatedById, CreatedDate, LastUpdatedById, LastUpdatedDate)" +
                                               " VALUES(@MessageGuid, @Payload, @TransactionId, @Publisher, @MetaData,@status,@messageEntityName," +
                                               "@FailedDateTime,@RetryCount,@Comments, " +
                                               "@CreatedById, @CreatedDate, @LastUpdatedById, @LastUpdatedDate)";
                    databaseConnection.Open();
                    databaseConnection.Execute(insertQuery, publishMessageLog);
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error inserting to MessageLog.");
                    return false;
                }
                finally
                {
                    databaseConnection.Close();
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Add(SubscriptionMessageLog messageLog)
        {
            using (IDbConnection databaseConnection = Connection)
            {
                try
                {
                    const string insertQuery = "INSERT INTO [msg].[SubscriptionMessageLog] (SubscriptionName,MessageGuid, Payload, TransactionId, " +
                                               "MetaData, Publisher, ReceivedDateTime, CreatedById, CreatedDate, " +
                                               "LastUpdatedById, LastUpdatedDate,ServiceBusEntityName)" +
                                               " VALUES(@SubscriptionName,@MessageGuid, @Payload, @TransactionId, @MetaData, @Publisher, @ReceivedDateTime," +
                                               "@CreatedById, @CreatedDate, @LastUpdatedById, @LastUpdatedDate,@ServiceBusEntityName)";
                    databaseConnection.Open();
                    databaseConnection.Execute(insertQuery, messageLog);
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error inserting to MessageLog.");
                    return false;
                }
                finally
                {
                    databaseConnection.Close();
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Add(FailedSubscriptionMessageLog messageLog)
        {
            using (IDbConnection databaseConnection = Connection)
            {
                try
                {
                    const string insertQuery = "INSERT INTO [msg].[FailedSubscriptionMessageLog] (SubscriptionName,MessageGuid, Payload, TransactionId, " +
                                               "MetaData, Publisher, FailedDateTime, Status, CreatedById, CreatedDate, " +
                                               "LastUpdatedById, LastUpdatedDate,ServiceBusEntityName,ErrorMessage)" +
                                               " VALUES(@SubscriptionName,@MessageGuid, @Payload, @TransactionId, @MetaData, @Publisher, @FailedDateTime," +
                                               "@Status,@CreatedById, @CreatedDate, @LastUpdatedById, @LastUpdatedDate,@ServiceBusEntityName,@ErrorMessage)";
                    databaseConnection.Open();
                    databaseConnection.Execute(insertQuery, messageLog);
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error inserting to MessageLog.");
                    return false;
                }
                finally
                {
                    databaseConnection.Close();
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public IEnumerable<T> GetAll<T>()
        {
            // Limiting 1000. we are not going to execute this from the code.
            using (var databaseConnection = Connection)
            {
                return databaseConnection.Query<T>("SELECT TOP 1000 * from [msg].[" + typeof(T).Name + "]");
            }
        }

        /// <inheritdoc/>
        public IList<MessageEntity> GetMessageEntity(string entityName, string owner)
        {
            var fromCache = GetEntityFromCache(new MessageEntityKey { EntityName = entityName, Owner = owner });
            if (fromCache != null)
            {
                var returnList = new List<MessageEntity>();
                _logger.LogDebug($"MessageEntity found based on entity {LoggerHelper.SanitizeValue(entityName)} and Owner {LoggerHelper.SanitizeValue(owner)} found in the cache ");
                returnList.Add(ReflectionHelper.Map<MessageEntity, MessageEntity>(fromCache)); // This was weird when I found it. Maybe to make a clone of the instance?
                return returnList;
            }

            using (var databaseConnection = Connection)
            {
                string searchQuery = "SELECT  MessageEntityId,NameSpace,PassiveNameSpace,ServiceBusEntityType, MaxRetryCount," +
                                "EntityName,MessageContext,Owner," +
                                "Status,Comments,CreatedById,CreatedDate,LastUpdatedById," +
                                "LastUpdatedDate,Version,IsPublishToServiceBusActive,EventHubNameSpace,IsPublishToEventHubActive FROM  [msg].[MessageEntity]" +
                                " WHERE EntityName = @ea AND Owner=@o ";
                IList<MessageEntity> returnList = databaseConnection.Query<MessageEntity>(searchQuery, new { ea = entityName, o = owner }).ToList();
                try
                {
                    if (returnList.Count > 0)
                    {
                        var entitySrc = returnList.FirstOrDefault();
                        if (!string.IsNullOrEmpty(entitySrc.NameSpace))
                        {
                            entitySrc.ConnectionString = _configuration.GetValue<string>($"ServiceBus:{entitySrc.NameSpace}");
                        }

                        if (!string.IsNullOrEmpty(entitySrc.PassiveNameSpace))
                        {
                            entitySrc.PassiveConnectionString = _configuration.GetValue<string>($"ServiceBus:{entitySrc.PassiveNameSpace}");
                        }

                        if (!string.IsNullOrEmpty(entitySrc.EventHubNameSpace))
                        {
                            entitySrc.EventHubConnectionString = _configuration.GetValue<string>($"EventHub:{entitySrc.EventHubNameSpace}");
                        }

                        var messageEntity = ReflectionHelper.Map<MessageEntity, MessageEntity>(entitySrc); // This was weird when I found it. Maybe to make a clone of the instance?
                        _cachedMessageEntity.TryAdd(new MessageEntityKey { EntityName = entityName, Owner = owner }, messageEntity);
                        _logger.LogDebug($"MessageEntity based on entity {LoggerHelper.SanitizeValue(entityName)} and Owner {LoggerHelper.SanitizeValue(owner)} added to cache ");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("MessageEntity can't be added to cache" +
                                                               $" based on entity {LoggerHelper.SanitizeValue(entityName)} and Owner {LoggerHelper.SanitizeValue(owner)} and Reason {e}");
                }

                return returnList;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByTransactionIds(string[] transactionIds)
        {
            try
            {
                using (var databaseConnection = Connection)
                {
                    var queryDesc = "SELECT FailedSubscriptionMessageLogId, ServiceBusEntityName, SubscriptionName, MessageGuid, TransactionId, MetaData, Publisher, FailedDateTime, Status, ErrorMessage, RetryCount " +
                                "FROM msg.FailedSubscriptionMessageLog WHERE TransactionId IN @TransactionIdValues AND IsActive = 1 ORDER BY FailedSubscriptionMessageLogId DESC";

                    return await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(queryDesc, new { TransactionIdValues = transactionIds }).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"GetFailedMessagesByTransactionIds failed for transactionIds: {string.Join(",", transactionIds)} with error: {e}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByFailedDate(DateTime failedDayStartTime, DateTime failedDayEndTime)
        {
            try
            {
                using (var databaseConnection = Connection)
                {
                    var queryDesc = "SELECT FailedSubscriptionMessageLogId, ServiceBusEntityName, SubscriptionName, MessageGuid, Payload, TransactionId, MetaData, Publisher, " +
                    "FailedDateTime, Status, ErrorMessage, RetryCount FROM msg.FailedSubscriptionMessageLog  WHERE FailedDateTime >= @FailedDayStartTime AND FailedDateTime < @FailedDayEndTime AND IsActive = 1 " +
                    "ORDER BY FailedSubscriptionMessageLogId DESC";

                    DynamicParameters param = new DynamicParameters();
                    param.Add("@FailedDayStartTime", failedDayStartTime, DbType.DateTime2);
                    param.Add("@FailedDayEndTime", failedDayEndTime, DbType.DateTime2);
                    return await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(queryDesc, param).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"GetFailedMessagesByFailedDate failed for start date: {failedDayStartTime} and end date: {failedDayEndTime} with error: {e}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByIds(long[] messageIds)
        {
            try
            {
                using (var databaseConnection = Connection)
                {
                    var queryDesc = "SELECT FailedSubscriptionMessageLogId, ServiceBusEntityName, SubscriptionName, Payload, Status FROM msg.FailedSubscriptionMessageLog  WHERE FailedSubscriptionMessageLogId IN @FailedSubscriptionMessageLogIds";
                    return await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(queryDesc, new { FailedSubscriptionMessageLogIds = messageIds }).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"GetFailedMessagesByIds failed for messageIds: {string.Join(",", messageIds)} with error: {e}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByTopicInDateRange(string topic, DateTime failedStartTime, DateTime failedEndTime)
        {
            try
            {
                using (var databaseConnection = Connection)
                {
                    var queryDesc = "SELECT FailedSubscriptionMessageLogId, ServiceBusEntityName, SubscriptionName, MessageGuid, Payload, TransactionId, MetaData, Publisher, " +
                    "FailedDateTime, Status, ErrorMessage, RetryCount FROM msg.FailedSubscriptionMessageLog  WHERE FailedDateTime >= @FailedDayStartTime AND FailedDateTime < @FailedDayEndTime AND ServiceBusEntityName = @ServiceBusEntityName AND IsActive = 1 " +
                    "ORDER BY FailedSubscriptionMessageLogId DESC";

                    DynamicParameters param = new DynamicParameters();
                    param.Add("@FailedDayStartTime", failedStartTime, DbType.DateTime2);
                    param.Add("@FailedDayEndTime", failedEndTime, DbType.DateTime2);
                    param.Add("@ServiceBusEntityName", topic, DbType.AnsiStringFixedLength);
                    var results = await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(queryDesc, param).ConfigureAwait(false);

                    if (results == null || !results.Any())
                    {
                        string serviceBusEntityName = $"%{topic}%";
                        queryDesc = "SELECT FailedSubscriptionMessageLogId, ServiceBusEntityName, SubscriptionName, MessageGuid, Payload, TransactionId, MetaData, Publisher, " +
                        "FailedDateTime, Status, ErrorMessage, RetryCount FROM msg.FailedSubscriptionMessageLog  WHERE FailedDateTime >= @FailedDayStartTime AND FailedDateTime < @FailedDayEndTime AND ServiceBusEntityName LIKE @ServiceBusEntityName AND IsActive = 1 " +
                        "ORDER BY FailedSubscriptionMessageLogId DESC";
                        results = await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(queryDesc, new { FailedDayStartTime = failedStartTime, FailedDayEndTime = failedEndTime, ServiceBusEntityName = serviceBusEntityName }).ConfigureAwait(false);
                    }

                    return results;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"GetFailedMessagesByTopicInDateRange failed for start date: {failedStartTime} and end date: {failedEndTime} with error: {e}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> UpdateFailedMessageStatus(long messageId, string status)
        {
            using (IDbConnection databaseConnection = Connection)
            {
                try
                {
                    string selectQuery = "SELECT FailedSubscriptionMessageLogId, RetryCount, ErrorMessage FROM msg.FailedSubscriptionMessageLog WHERE FailedSubscriptionMessageLogId = @FailedSubscriptionMessageLogId";
                    string updateQuery = "UPDATE msg.FailedSubscriptionMessageLog SET Status = @statusValue, RetryCount = @RetryCount, ErrorMessage = @ErrorMessage WHERE FailedSubscriptionMessageLogId = @FailedSubscriptionMessageLogId";

                    var messages = await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(selectQuery, new { FailedSubscriptionMessageLogId = messageId }).ConfigureAwait(false);

                    if (messages.Count() != 1)
                    {
                        var exception = new ExosMessagingException($"One and only one record should exist to update the status. There is a mismatch in the number of records for message ID: {messageId}");
                        _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, exception, "Error in UpdateFailedMessageStatus.");
                        return 0;
                    }

                    short retryCount = ++messages.ElementAt(0).RetryCount;
                    string errorMessage = $"Retry {retryCount} Status: {status}{Environment.NewLine} Original error message: {messages.ElementAt(0).ErrorMessage}";
                    var affectedRows = await databaseConnection.ExecuteAsync(updateQuery, new { FailedSubscriptionMessageLogId = messageId, statusValue = status, RetryCount = retryCount, ErrorMessage = errorMessage }).ConfigureAwait(false);
                    return affectedRows;
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error in UpdateFailedMessageStatus.");
                    return 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> UpdateFailedMessageErrorMessage(long messageId, string message)
        {
            using (IDbConnection databaseConnection = Connection)
            {
                try
                {
                    string selectQuery = "SELECT FailedSubscriptionMessageLogId, RetryCount, ErrorMessage FROM msg.FailedSubscriptionMessageLog WHERE FailedSubscriptionMessageLogId = @FailedSubscriptionMessageLogId";
                    string updateQuery = "UPDATE msg.FailedSubscriptionMessageLog SET ErrorMessage = @ErrorMessage, RetryCount = @RetryCount WHERE FailedSubscriptionMessageLogId = @FailedSubscriptionMessageLogId";

                    var messages = await databaseConnection.QueryAsync<FailedSubscriptionMessageLog>(selectQuery, new { FailedSubscriptionMessageLogId = messageId }).ConfigureAwait(false);

                    if (messages.Count() != 1)
                    {
                        var e = new ExosMessagingException($"One and only one record should exist to update the error message. There is a mismatch in the number of records for message ID: {messageId}");
                        _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error in UpdateFailedMessageErrorMessage.");
                        return 0;
                    }

                    short retryCount = ++messages.ElementAt(0).RetryCount;
                    string errorMessage = $"Retry {retryCount} Error message: {messages.ElementAt(0).ErrorMessage}{Environment.NewLine} Original error message: {message}";
                    var affectedRows = await databaseConnection.ExecuteAsync(updateQuery, new { FailedSubscriptionMessageLogId = messageId, ErrorMessage = errorMessage, RetryCount = retryCount }).ConfigureAwait(false);
                    return affectedRows;
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error in UpdateFailedMessageErrorMessage.");
                    return 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> UpdateFailedMessageIsActive(long[] messageIds, bool isActive)
        {
            using (IDbConnection databaseConnection = Connection)
            {
                try
                {
                    var failedMessagesToArchive = await GetFailedMessagesByIds(messageIds).ConfigureAwait(false);
                    if (failedMessagesToArchive != null && messageIds != null && failedMessagesToArchive.Count() == messageIds.Length)
                    {
                        string updateQuery = "UPDATE msg.FailedSubscriptionMessageLog SET IsActive = @IsActive WHERE FailedSubscriptionMessageLogId IN @FailedSubscriptionMessageLogIds";
                        var affectedRows = await databaseConnection.ExecuteAsync(updateQuery, new { FailedSubscriptionMessageLogIds = messageIds, IsActive = isActive }).ConfigureAwait(false);
                        return affectedRows;
                    }
                    else
                    {
                        var exception = new ExosMessagingException($"There is a mismatch in the number of records for message ID: {string.Join(",", messageIds)}");
                        _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, exception, "Error in UpdateFailedMessageIsActive.");
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error in UpdateFailedMessageIsActive.");
                    return 0;
                }
            }
        }

        /// <summary>
        /// Get Message Entity from Cache.
        /// </summary>
        /// <param name="key">MessageEntityKey.</param>
        /// <returns>Message Entity from Cache, null if not found.</returns>
        private static MessageEntity GetEntityFromCache(MessageEntityKey key)
        {
            MessageEntity entity = null;
            if (_cachedMessageEntity.ContainsKey(key))
            {
                _cachedMessageEntity.TryGetValue(key, out entity);
            }

            // If entity namespace is null, then the object is corrupted. remove it
            if (entity != null && entity.ConnectionString != null)
            {
                return entity;
            }
            else
            {
                _cachedMessageEntity.TryRemove(key, out entity);
                return null;
            }
        }

        /// <summary>
        /// Parse the database connection string.
        /// </summary>
        /// <param name="connectionString">Connection String.</param>
        /// <returns>Returns the first parameter of the database connection string.</returns>
        private string MessageDbString(string connectionString)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    return connectionString.Split(";")[0];
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Connection string parsing error");
                }
            }

            return null;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
