namespace Exos.Platform.Persistence.EventTracking
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Persistence.Models;

    /// <summary>
    /// Service to process tracking events.
    /// </summary>
    public interface IEventTrackingService
    {
        /// <summary>
        /// Create a list of events.
        /// </summary>
        /// <param name="platformDbContext">DbContext.</param>
        /// <returns>List of events.</returns>
        List<EventTrackingEntry> CreateEventTracking(PlatformDbContext platformDbContext);

        /// <summary>
        /// Update a list of events asynchronously.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of events to update.</param>
        /// <param name="platformDbContext">DbContext.</param>
        /// <returns>List of updated events.</returns>
        Task UpdateEventTrackingAsync(List<EventTrackingEntry> eventTrackingEntryList, PlatformDbContext platformDbContext);

        /// <summary>
        /// Update a list of events.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of events to update.</param>
        /// <param name="platformDbContext">DbContext.</param>
        /// <returns>List of updated events.</returns>
        int UpdateEventTracking(List<EventTrackingEntry> eventTrackingEntryList, PlatformDbContext platformDbContext);

        /// <summary>
        /// Cancel future events, one single event.
        /// </summary>
        /// <param name="explictEvent">Explicit Event.</param>
        /// <param name="cancelEvent">Cancellation Event.</param>
        /// <returns>Canceled events.</returns>
        Task CancelFutureEvent(ExplicitEvent explictEvent, CancelEvent cancelEvent);

        /// <summary>
        /// Cancel future events.
        /// </summary>
        /// <param name="explictEvent">Explicit event.</param>
        /// <param name="futureEventConfig">Event configuration.</param>
        /// <returns>Canceled events.</returns>
        Task CancelFutureEvents(ExplicitEvent explictEvent, EventConfig futureEventConfig);

        /// <summary>
        /// Cancel events using a key (value in PrimaryKeyValue column).
        /// </summary>
        /// <param name="primaryKeyValue">Event primary key.</param>
        /// <returns>Canceled events.</returns>
        Task CancelFutureEvents(string primaryKeyValue);

        /// <summary>
        /// Generate one future event.
        /// </summary>
        /// <param name="explictEvent">Explicit event.</param>
        /// <param name="futureEvent">Future event.</param>
        /// <returns>EventTrackingEntity.</returns>
        Task<EventTrackingEntity> GenerateFutureEvent(ExplicitEvent explictEvent, FutureEvent futureEvent);

        /// <summary>
        /// Generate Future Events.
        /// </summary>
        /// <param name="explictEvent">Explicit event.</param>
        /// <param name="futureEventConfig">Future event configuration.</param>
        /// <returns>Generated Events.</returns>
        Task GenerateFutureEvents(ExplicitEvent explictEvent, EventConfig futureEventConfig);

        /// <summary>
        /// Create explicit (not persistence) events.
        /// </summary>
        /// <param name="explictEvents">Events.</param>
        /// <returns>List of EventTrackingEntity.</returns>
        Task<List<EventTrackingEntity>> CreateExplicitEvents(List<ExplicitEvent> explictEvents);

        /// <summary>
        /// Create an explicit (not persistence) event.
        /// </summary>
        /// <param name="explictEvent">Event.</param>
        /// <returns>EventTrackingEntity.</returns>
        Task<EventTrackingEntity> CreateExplicitEvent(ExplicitEvent explictEvent);

        /// <summary>
        /// Create an explicit (not persistence) event.
        /// </summary>
        /// <param name="explictEvent">Event.</param>
        /// <param name="eventConfig">Event configuration.</param>
        /// <returns>EventTrackingEntity.</returns>
        Task<EventTrackingEntity> CreateExplicitEvent(ExplicitEvent explictEvent, EventConfig eventConfig);

        /// <summary>
        /// Add Meta-data to event.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        void AddMetadata(string key, object value);
    }
}
