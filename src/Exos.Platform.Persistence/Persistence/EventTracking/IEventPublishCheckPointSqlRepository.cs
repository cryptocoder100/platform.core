namespace Exos.Platform.Persistence.EventTracking
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Persistence.EventPoller;

    /// <summary>
    /// Repository for Event Publish Check Point.
    /// </summary>
    /// <typeparam name="T">Event Type.</typeparam>
    public interface IEventPublishCheckPointSqlRepository<T> where T : EventPublishCheckPointEntity
    {
        /// <summary>
        /// CreateEventCheckPoints.
        /// </summary>
        /// <param name="checkPoints">Check Point Events.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CreateEventCheckPoints(List<T> checkPoints);

        /// <summary>
        /// Update Event.
        /// </summary>
        /// <param name="checkPoint">Check Point Event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<T> UpdateEventCheckPoint(T checkPoint);

        /// <summary>
        /// Delete Events.
        /// </summary>
        /// <param name="deleteQuery">Delete Query.</param>
        /// <param name="eventIds">List of event Ids.</param>
        /// <param name="processId">processId.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteEventCheckPoints(string deleteQuery, int[] eventIds, byte processId);

        /// <summary>
        /// Delete Events physically from the Event Checkpoint Table.
        /// </summary>
        /// <param name="eventPublishCheckPoinHardDeleteQuery">Query to delete Events.</param>
        /// <param name="eventIds">eventIds.</param>
        /// <returns>Number of deleted rows.</returns>
        Task<int> HardDeleteEventCheckpoints(string eventPublishCheckPoinHardDeleteQuery, int[] eventIds);

        /// <summary>
        /// Get Event Ids which needs deleted.
        /// </summary>
        /// <param name="eventIdsToBeDeletedQuery">Delete Query.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<int>> GetEventIdsToBeDeleted(string eventIdsToBeDeletedQuery);
    }
}
