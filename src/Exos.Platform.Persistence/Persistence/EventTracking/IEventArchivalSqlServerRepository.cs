namespace Exos.Platform.Persistence.EventTracking
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Repository to archive events.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public interface IEventArchivalSqlServerRepository<T> where T : EventTrackingEntity
    {
        /// <summary>
        /// Archive events.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of events.</param>
        /// <returns>Archived events.</returns>
        Task<int> ArchiveEvents(List<T> eventTrackingEntryList);
    }
}
