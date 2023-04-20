#pragma warning disable SA1402 // FileMayOnlyContainASingleType
namespace Exos.MinimalPlatformService.Models
{
    using System;

    /// <summary>
    /// Service Status.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Initializing Status.
        /// </summary>
        Initializing,

        /// <summary>
        /// Starting Status.
        /// </summary>
        Starting,

        /// <summary>
        /// Running Status.
        /// </summary>
        Running,

        /// <summary>
        /// Stopping Status.
        /// </summary>
        Stopping,

        /// <summary>
        /// Stopped Status.
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// Status Model.
    /// </summary>
    public class StatusModel
    {
        /// <summary>
        /// Gets Status.
        /// </summary>
        public Status Status
        {
            get
            {
                if (PollingService.Status <= SubscriptionService.Status)
                {
                    return PollingService.Status;
                }
                else
                {
                    return SubscriptionService.Status;
                }
            }
        }

        /// <summary>
        /// Gets Polling Service Status.
        /// </summary>
        public ServiceStatusModel PollingService { get; } = new ServiceStatusModel();

        /// <summary>
        /// Gets Subscription Service Status.
        /// </summary>
        public ServiceStatusModel SubscriptionService { get; } = new ServiceStatusModel();
    }

    /// <summary>
    /// Service Status Model.
    /// </summary>
    public class ServiceStatusModel
    {
        /// <summary>
        /// Gets or sets the Status of the services.
        /// </summary>
        public Status Status { get; set; } = Status.Initializing;

        /// <summary>
        /// Gets or sets the time when the service starts.
        /// </summary>
        public DateTimeOffset? Started { get; set; }

        /// <summary>
        /// Gets for how long the service is running for.
        /// </summary>
        public TimeSpan? RunningFor => Started.HasValue ? DateTimeOffset.UtcNow - Started : default(TimeSpan?);

        /// <summary>
        /// Gets or sets Actions.
        /// </summary>
        public long Actions { get; set; }
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType