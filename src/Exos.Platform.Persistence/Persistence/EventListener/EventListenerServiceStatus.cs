namespace Exos.Platform.Persistence.EventListener
{
    using System;

    /// <summary>
    /// The possible states for EventPollerService.
    /// </summary>
    public enum EventListenerServiceState
    {
        /// <summary>
        /// Initializing the Event Listener Service.
        /// </summary>
        Initializing,

        /// <summary>
        /// Starting the Event Listener Service.
        /// </summary>
        Starting,

        /// <summary>
        /// Running the Event Listener Service.
        /// </summary>
        Running,

        /// <summary>
        /// Stopping the Event Listener Service.
        /// </summary>
        Stopping,

        /// <summary>
        /// Stopped the Event Listener Service.
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// Status information for EventListenerService.
    /// </summary>
    public class EventListenerServiceStatus
    {
        /// <summary>
        /// Gets or sets the current state of EventListenerService.
        /// </summary>
        public EventListenerServiceState State { get; set; } = EventListenerServiceState.Initializing;

        /// <summary>
        /// Gets or sets when was EventListenerService started.
        /// </summary>
        public DateTimeOffset? Started { get; set; }

        /// <summary>
        /// Gets how long has EventListenerService been running.
        /// </summary>
        public TimeSpan? RunningFor => Started.HasValue ? DateTimeOffset.UtcNow - Started : default(TimeSpan?);

        /// <summary>
        /// Gets or sets how many times has the polling loop logic been invoked.
        /// </summary>
        public long PollingExecutions { get; set; }
    }
}
