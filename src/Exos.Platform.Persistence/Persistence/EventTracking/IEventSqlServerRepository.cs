namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Repository to access events in the event table.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public interface IEventSqlServerRepository<T> where T : EventTrackingEntity
    {
        /// <summary>
        /// Create a event.
        /// </summary>
        /// <param name="trackingEvent">Event to create.</param>
        /// <returns>Created event.</returns>
        Task<T> CreateEvent(T trackingEvent);

        /// <summary>
        /// Create events.
        /// </summary>
        /// <param name="trackingEvents">Events to create.</param>
        /// <returns>Created events.</returns>
        Task<List<T>> CreateEvents(List<T> trackingEvents);

        /// <summary>
        /// Find events. This method dispose the connection.
        /// </summary>
        /// <param name="query">Query to find events.</param>
        /// <returns>List of events that match the query condition.</returns>
        Task<List<T>> FindEvents(string query);

        /// <summary>
        /// Find events by primary key.This does not dispose the connection.
        /// </summary>
        /// <param name="primaryKeyValue">Primary key value.</param>
        /// <returns>List of events matching the primary key.</returns>
        Task<List<T>> QueryEvents(string primaryKeyValue);

        /// <summary>
        /// Query Events by Event Name and Primary Key.
        /// </summary>
        /// <param name="eventName">Event Name.</param>
        /// <param name="primaryKeyValue">Primary Key value.</param>
        /// <returns>List of events that match the condition.</returns>
        Task<List<T>> QueryEvents(string eventName, string primaryKeyValue);

        /// <summary>
        /// Update the status of the events, updating the isActive flag.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events to update.</param>
        /// <param name="isActive">IsActive value.</param>
        /// <returns>Updated events.</returns>
        Task<int> UpdateEventStatus(List<T> eventTrackingEntryList, bool isActive);

        /// <summary>
        /// Update the status of the events, updating the isActive flag.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events to update.</param>
        /// <param name="isActive">IsActive value.</param>
        /// <param name="updateQuery">Update statement to update the events.</param>
        /// <param name="batchSize">Number of records to update in each update.</param>
        /// <returns>Updated events.</returns>
        Task<int> UpdateEventStatus(List<T> eventTrackingEntryList, bool isActive, string updateQuery, int batchSize = 2000);

        /// <summary>
        /// Query event using a specific query.
        /// </summary>
        /// <param name="query">Query to read events.</param>
        /// <param name="dueDate">Due Data for events.</param>
        /// <param name="eventId">Event Id to query.</param>
        /// <returns>List of Events matching the query.</returns>
        Task<List<T>> QueryEvents(string query, DateTimeOffset dueDate, int eventId);

        /// <summary>
        /// Delete Events physically from the Event Queue Table.
        /// </summary>
        /// <param name="eventQueueHardDeleteQuery">Query to delete Events.</param>
        /// <param name="eventIds">eventIds.</param>
        /// <returns>Number of deleted rows.</returns>
        Task<int> HardDeleteEvents(string eventQueueHardDeleteQuery, int[] eventIds);

        /// <summary>
        /// Query event using a specific query.
        /// </summary>
        /// <param name="query">Query to read events.</param>
        /// <param name="processId">processId for events.</param>
        /// <returns>List of Events matching the query.</returns>
        Task<List<T>> QueryEvents(string query, byte processId);
    }
}