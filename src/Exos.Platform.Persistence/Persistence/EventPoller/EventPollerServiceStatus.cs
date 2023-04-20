namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Globalization;

    /// <summary>
    /// The possible states for EventPollerService.
    /// </summary>
    public enum EventPollerServiceState
    {
        /// <summary>
        /// Initializing the Event Poller Service.
        /// </summary>
        Initializing,

        /// <summary>
        /// Starting the Event Poller Service.
        /// </summary>
        Starting,

        /// <summary>
        ///  Running the Event Poller Service.
        /// </summary>
        Running,

        /// <summary>
        /// Stopping the Event Poller Service.
        /// </summary>
        Stopping,

        /// <summary>
        /// Stopped Event Poller Service.
        /// </summary>
        Stopped,

        /// <summary>
        /// Scheduled Event Poller Service.
        /// </summary>
        Scheduled,

        /// <summary>
        /// Finished Event Poller Service.
        /// </summary>
        Finished,

        /// <summary>
        /// Disabled Event Poller Service.
        /// </summary>
        Disabled,
    }

    /// <summary>
    /// Status information for EventPollerService.
    /// </summary>
    public class EventPollerServiceStatus
    {
        /// <summary>
        /// Gets or sets event Poller Name.
        /// </summary>
        public string EventPollerName { get; set; }

        /// <summary>
        /// Gets or sets the current state of EventPollerService.
        /// </summary>
        public EventPollerServiceState State { get; set; } = EventPollerServiceState.Initializing;

        /// <summary>
        /// Gets or sets when was EventPollerService started.
        /// </summary>
        public DateTimeOffset? Started { get; set; }

        /// <summary>
        /// Gets how long has EventPollerService been running.
        /// </summary>
        public TimeSpan? RunningFor => Started.HasValue ? DateTimeOffset.UtcNow - Started : default(TimeSpan?);

        /// <summary>
        /// Gets or sets how many times has the polling loop logic been invoked.
        /// </summary>
        public long PollingExecutions { get; set; }

        /// <summary>
        /// Gets or sets last Execution EventPollerService started.
        /// </summary>
        public DateTimeOffset? LastExecution { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (State == EventPollerServiceState.Stopped || State == EventPollerServiceState.Disabled)
            {
                return $@"{{{Environment.NewLine}  ""Name"":""{EventPollerName}"",{Environment.NewLine}  ""State"":""{State}"",{Environment.NewLine}""}}";
            }
            else if (State == EventPollerServiceState.Running)
            {
                return $@"{{{Environment.NewLine}  ""Name"":""{EventPollerName}"",{Environment.NewLine}  ""State"":""{State}"",{Environment.NewLine}  ""Started"":""{(Started.HasValue ? Started.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty)}"",{Environment.NewLine}  ""Last Execution"":""{(LastExecution.HasValue ? LastExecution.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty)}"",{Environment.NewLine}  ""Running For"":""{(RunningFor.HasValue ? RunningFor.Value.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture) : string.Empty)}"",{Environment.NewLine}  ""Number of Executions"":""{PollingExecutions.ToString(CultureInfo.InvariantCulture)}""{Environment.NewLine}}}";
            }
            else if (State == EventPollerServiceState.Initializing)
            {
                return $@"{{{Environment.NewLine}  ""Name"":""Event Poller Service"",{Environment.NewLine}  ""State"":""Not Running / Not Started""{Environment.NewLine}}}";
            }
            else
            {
                return $@"{{{Environment.NewLine}  ""Name"":""{EventPollerName}"",{Environment.NewLine}  ""State"":""{State}"",{Environment.NewLine}  ""Started"":""{(Started.HasValue ? Started.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty)}"",{Environment.NewLine}  ""Last Execution"":""{(LastExecution.HasValue ? LastExecution.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty)}"",{Environment.NewLine}  ""Number of Executions"":""{PollingExecutions.ToString(CultureInfo.InvariantCulture)}""{Environment.NewLine}}}";
            }
        }
    }
}
