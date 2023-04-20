namespace Exos.Platform.ICTLibrary.Repository
{
    using System.Threading.Tasks;
    using Exos.Platform.ICTLibrary.Repository.Model;

    /// <summary>
    /// Data access to ICT database.
    /// </summary>
    public interface IIctRepository
    {
        /// <summary>
        /// Get Event Entity Topic.
        /// </summary>
        /// <param name="entityName">Entity Name.</param>
        /// <param name="eventName">Event Name.</param>
        /// <param name="applicationId">Application Id.</param>
        /// <returns>Event Entity Topic.</returns>
        Task<EventEntityTopic> GetEventEntityTopic(string entityName, string eventName, short applicationId);

        /// <summary>
        /// Get Event Entity Topic.
        /// </summary>
        /// <param name="entityName">Entity Name.</param>
        /// <param name="eventName">Event Name.</param>
        /// <param name="applicationName">Application Name.</param>
        /// <returns>Event Entity Topic.</returns>
        Task<EventEntityTopic> GetEventEntityTopic(string entityName, string eventName, string applicationName);

        /// <summary>
        /// Add EventTracking entity to database.
        /// </summary>
        /// <param name="eventTracking">Event Tracking.</param>
        /// <returns>Completed task.</returns>
        Task AddEventTracking(EventTracking eventTracking);
    }
}
