namespace Exos.Platform.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Repository.Model;

    /// <summary>
    /// Service to Manage Failed Messages.
    /// </summary>
    public interface IFailedMessageService
    {
        /// <summary>
        /// Get All Failed Messages.
        /// </summary>
        /// <returns>List of Failed Messages.</returns>
        IEnumerable<FailedSubscriptionMessageLog> GetAllFailedMessages();

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
