namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapper;
    using Exos.Platform.AspNetCore.Helpers;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    /// <summary>
    /// Repository to access events in the event table.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public class EventSqlServerRepository<T> : IEventSqlServerRepository<T> where T : EventTrackingEntity
    {
        private readonly ILogger<EventSqlServerRepository<T>> _logger;

        private readonly DbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSqlServerRepository{T}"/> class.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider instance.</param>
        /// <param name="logger">Logger instance.</param>
        public EventSqlServerRepository(IServiceProvider serviceProvider, ILogger<EventSqlServerRepository<T>> logger)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _dbContext = serviceProvider.GetService<PlatformDbContext>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a event.
        /// </summary>
        /// <param name="trackingEvent">Event to create.</param>
        /// <returns>Created event.</returns>
        public async Task<T> CreateEvent(T trackingEvent)
        {
            await _dbContext.AddAsync(trackingEvent).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return trackingEvent;
        }

        /// <summary>
        /// Create events.
        /// </summary>
        /// <param name="trackingEvents">Events to create.</param>
        /// <returns>Created events.</returns>
        public async Task<List<T>> CreateEvents(List<T> trackingEvents)
        {
            await _dbContext.AddRangeAsync(trackingEvents).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return trackingEvents;
        }

        /// <summary>
        /// Find events.
        /// </summary>
        /// <param name="query">Query to find events.</param>
        /// <returns>List of events that match the query condition.</returns>
        public async Task<List<T>> FindEvents(string query)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    var events = await connection.QueryAsync<T>(query).ConfigureAwait(false);
                    return events.AsList<T>();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database exception in EventSqlServerRepository  {message} - method FindEvents - {query}", e.Message, LoggerHelper.SanitizeValue(query));
                throw;
            }
        }

        /// <summary>
        /// Find events by primary key.
        /// </summary>
        /// <param name="primaryKeyValue">Primary key value.</param>
        /// <returns>List of events matching the primary key.</returns>
        public Task<List<T>> QueryEvents(string primaryKeyValue)
        {
            return _dbContext.Set<T>().Where(i => i.IsActive && i.PrimaryKeyValue == primaryKeyValue).ToListAsync<T>();
        }

        /// <summary>
        /// Query Events by Event Name and Primary Key.
        /// </summary>
        /// <param name="eventName">Event Name.</param>
        /// <param name="primaryKeyValue">Primary Key value.</param>
        /// <returns>List of events that match the condition.</returns>
        public Task<List<T>> QueryEvents(string eventName, string primaryKeyValue)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            if (string.IsNullOrEmpty(primaryKeyValue))
            {
                throw new ArgumentNullException(nameof(primaryKeyValue));
            }

            return _dbContext.Set<T>().Where(i => i.IsActive && i.EventName == eventName && i.PrimaryKeyValue == primaryKeyValue).ToListAsync<T>();
        }

        /// <summary>
        /// Update the status of the events, updating the isActive flag.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events to update.</param>
        /// <param name="isActive">IsActive value.</param>
        /// <returns>Updated events.</returns>
        public async Task<int> UpdateEventStatus(List<T> eventTrackingEntryList, bool isActive)
        {
            if (eventTrackingEntryList == null)
            {
                throw new ArgumentNullException(nameof(eventTrackingEntryList));
            }

            foreach (var eventTrackingEntry in eventTrackingEntryList)
            {
                eventTrackingEntry.IsActive = isActive;
            }

            _dbContext.UpdateRange(eventTrackingEntryList);
            var updatedRows = await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return updatedRows;
        }

        /// <summary>
        /// Update the status of the events, updating the isActive flag.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events to update.</param>
        /// <param name="isActive">IsActive value.</param>
        /// <param name="updateQuery">Update statement to update the events.</param>
        /// <param name="batchSize">Number of records to update in each update.</param>
        /// <returns>Updated events.</returns>
        public async Task<int> UpdateEventStatus(List<T> eventTrackingEntryList, bool isActive, string updateQuery, int batchSize = 2000)
        {
            if (eventTrackingEntryList == null)
            {
                throw new ArgumentNullException(nameof(eventTrackingEntryList));
            }

            // Extract the list of event id's
            List<int> eventIdList = eventTrackingEntryList.ConvertAll(e => e.EventId);

            // Create a list of event Id's
            List<List<int>> eventIdBatchList = Batch<int>(eventIdList, batchSize);
            string eventIdWhere;
            string updateStmt;
            int updatedRows = 0;
            using (var connection = _dbContext.Database.GetDbConnection())
            {
                foreach (var eventIdBatchItem in eventIdBatchList)
                {
                    eventIdWhere = @" WHERE EventId IN(" + string.Join(",", eventIdBatchItem) + ")";
                    updateStmt = @updateQuery + eventIdWhere;
                    updatedRows += await connection.ExecuteAsync(updateStmt, new { isActive }).ConfigureAwait(false);
                }
            }

            return updatedRows;
        }

        /// <summary>
        /// Query event using a specific query.
        /// </summary>
        /// <param name="query">Query to read events.</param>
        /// <param name="dueDate">Due Data for events.</param>
        /// <param name="eventId">Event Id to query.</param>
        /// <returns>List of Events matching the query.</returns>
        public async Task<List<T>> QueryEvents(string query, DateTimeOffset dueDate, int eventId)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@EventId", eventId);
                    parameters.Add("@DueDate", dueDate.UtcDateTime, System.Data.DbType.DateTime2);

                    var events = await connection.QueryAsync<T>(query, parameters).ConfigureAwait(false);
                    return events.AsList<T>();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database exception in EventSqlServerRepository  {message} - method QueryEvents - {query}", e.Message, LoggerHelper.SanitizeValue(query));
                throw;
            }
        }

        /// <summary>
        /// Delete Events physically from the Event Queue Table.
        /// </summary>
        /// <param name="eventQueueHardDeleteQuery">Query to delete Events.</param>
        /// <param name="eventIds">eventIds.</param>
        /// <returns>Number of deleted rows.</returns>
        public async Task<int> HardDeleteEvents(string eventQueueHardDeleteQuery, int[] eventIds)
        {
            int deletedRows = 0;
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                deletedRows = await connection.ExecuteAsync(eventQueueHardDeleteQuery, new { EventIds = eventIds }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database exception in EventSqlServerRepository  {message} - method HardDeleteEvents - {query}", e.Message, LoggerHelper.SanitizeValue(eventQueueHardDeleteQuery));
                throw;
            }

            return deletedRows;
        }

        /// <summary>
        /// Query event using a specific query.
        /// </summary>
        /// <param name="query">Query to read events.</param>
        /// <param name="processId">processId to query.</param>
        /// <returns>List of Events matching the query.</returns>
        public async Task<List<T>> QueryEvents(string query, byte processId)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@ProcessId", processId, System.Data.DbType.Byte);
                    var events = await connection.QueryAsync<T>(query, parameters).ConfigureAwait(false);
                    return events.AsList<T>();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database exception in EventSqlServerRepository  {message} - method QueryEvents - {query}", e.Message, LoggerHelper.SanitizeValue(query));
                throw;
            }
        }

        /// <summary>
        /// Get a list of items by batch size.
        /// </summary>
        /// <typeparam name="TL">Item type.</typeparam>
        /// <param name="items">List of items.</param>
        /// <param name="batchSize">Batch size.</param>
        /// <returns>List of items by batch size.</returns>
        private static List<List<TL>> Batch<TL>(List<TL> items, int batchSize)
        {
            var returnList = new List<List<TL>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                returnList.Add(items.GetRange(i, Math.Min(batchSize, items.Count - i)));
            }

            return returnList;
        }
    }
}
