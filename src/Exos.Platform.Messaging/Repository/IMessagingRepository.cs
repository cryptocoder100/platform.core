namespace Exos.Platform.Messaging.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Repository.Model;

    /// <summary>
    /// Messaging Database repository.
    /// </summary>
    public interface IMessagingRepository
    {
        /// <summary>
        /// Gets Database Connection.
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// Add a Message Log Entry.
        /// </summary>
        /// <param name="messageLog">MessageLog.</param>
        /// <returns>True if entry was added.</returns>
        bool Add(MessageLog messageLog);

        /// <summary>
        /// Add a Error Message Log Entry.
        /// </summary>
        /// <param name="publishMessageLog">PublishErrorMessageLog.</param>
        /// <returns>True if entry was added.</returns>
        bool Add(PublishErrorMessageLog publishMessageLog);

        /// <summary>
        /// Add a Subscription Message Log.
        /// </summary>
        /// <param name="subscriptionMessageLog">SubscriptionMessageLog.</param>
        /// <returns>True if entry was added.</returns>
        bool Add(SubscriptionMessageLog subscriptionMessageLog);

        /// <summary>
        /// Add a Failed Subscription Message Log.
        /// </summary>
        /// <param name="publishMessageLog">FailedSubscriptionMessageLog.</param>
        /// <returns>True if entry was added.</returns>
        bool Add(FailedSubscriptionMessageLog publishMessageLog);

        /// <summary>
        /// Get all the objects.
        /// </summary>
        /// <typeparam name="T">Object to get.</typeparam>
        /// <returns>List of objects.</returns>
        IEnumerable<T> GetAll<T>();

        /// <summary>
        /// Get all message entities based on topic/queue and owner.
        /// </summary>
        /// <param name="entityName">Entity Name.</param>
        /// <param name="owner">Entity Owner.</param>
        /// <returns>List of Message Entity.</returns>
        IList<MessageEntity> GetMessageEntity(string entityName, string owner);

        /// <summary>
        /// Get Failed Messages by Transaction Id.
        /// </summary>
        /// <param name="transactionIds">List of Transaction Id.</param>
        /// <returns>List of failed messages filtered by Transaction Id.</returns>
        Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByTransactionIds(string[] transactionIds);

        /// <summary>
        /// Get  Failed Message by Date Range.
        /// </summary>
        /// <param name="failedDayStartTime">Start Date Time.</param>
        /// <param name="failedDayEndTime">Ending Date Time.</param>
        /// <returns>>List of failed messages filtered Date Time.</returns>
        Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByFailedDate(DateTime failedDayStartTime, DateTime failedDayEndTime);

        /// <summary>
        /// Get Failed Messages by Message Id.
        /// </summary>
        /// <param name="messageIds">List of Message Id.</param>
        /// <returns>List of failed messages matching the Message Id list.</returns>
        Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByIds(long[] messageIds);

        /// <summary>
        /// Update Failed Message Status.
        /// </summary>
        /// <param name="messageId">Message Id.</param>
        /// <param name="status">Status to Update.</param>
        /// <returns>Number of updated messages.</returns>
        Task<int> UpdateFailedMessageStatus(long messageId, string status);

        /// <summary>
        /// Update Failed Message.
        /// </summary>
        /// <param name="messageId">Message Id.</param>
        /// <param name="message">Error message to update.</param>
        /// <returns>Number of updated messages.</returns>
        Task<int> UpdateFailedMessageErrorMessage(long messageId, string message);

        /// <summary>
        /// Get Failed Message by Topic and Date range.
        /// </summary>
        /// <param name="topic">Topic.</param>
        /// <param name="failedStartTime">Start Date.</param>
        /// <param name="failedEndTime">End Date.</param>
        /// <returns>List of Failed Messages.</returns>
        Task<IEnumerable<FailedSubscriptionMessageLog>> GetFailedMessagesByTopicInDateRange(string topic, DateTime failedStartTime, DateTime failedEndTime);

        /// <summary>
        /// Update Active Flag in Failed Message.
        /// </summary>
        /// <param name="messageIds">List of Message Id.</param>
        /// <param name="isActive">Is Active?.</param>
        /// <returns>Number of updated messages.</returns>
        Task<int> UpdateFailedMessageIsActive(long[] messageIds, bool isActive);
    }
}
