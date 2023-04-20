namespace Exos.Platform.ICTLibrary.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.ICTLibrary.Core.Model;

    /// <summary>
    /// ICT Event Publisher.
    /// </summary>
    public interface IIctEventPublisher
    {
        /// <summary>
        /// Publish Event to ICT.
        /// </summary>
        /// <param name="ictEventMessage">ICT Event Message.</param>
        /// <param name="isTrackingInDbEnabled">Indicate if the events are tracked in the ICT EventTracking table.</param>
        /// <returns>True if message was published.</returns>
        Task<bool> PublishEvent(IctEventMessage ictEventMessage, bool isTrackingInDbEnabled = false);

        /// <summary>
        /// Publish a list of Events to ICT.
        /// </summary>
        /// <param name="ictEventMessages">The ictEventMessages<see cref="List{IctEventMessage}"/>.</param>
        /// <param name="isTrackingInDbEnabled">Indicate if the events are tracked in the ICT EventTracking table.</param>
        /// <returns>True if message list was published.</returns>
        Task<IList<IctEventMessage>> PublishEvents(List<IctEventMessage> ictEventMessages, bool isTrackingInDbEnabled = false);
    }
}
