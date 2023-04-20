namespace Exos.Platform.Persistence.EventPoller
{
    using System.Threading.Tasks;

    /// <summary>
    /// Process persistent events sending events using ICT publisher.
    /// </summary>
    public interface IICTEventPollerService
    {
        /// <summary>
        /// Gets service status.
        /// </summary>
        /// <returns>Service status.</returns>
        EventPollerServiceStatus ICTEventPollerServiceStatus();

        /// <summary>
        /// Execute event processing.
        /// </summary>
        /// <returns>Events processed.</returns>
        Task RunAsync();
    }

    /// <summary>
    /// Interface for Service Bus.
    /// </summary>
    public interface IICTEventPollerCheckPointServiceBusService : IICTEventPollerService
    {
        /// <summary>
        /// Delete old events.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task HardDeleteOldEvents();
    }

    /// <summary>
    /// Interface for Event Hub.
    /// </summary>
    public interface IICTEventPollerCheckPointEventHubService : IICTEventPollerService
    {
        /// <summary>
        /// Delete old events.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task HardDeleteOldEvents();
    }

    /// <summary>
    /// Interface for Event Hub.
    /// </summary>
    public interface IICTEventPollerCheckPointIntegrationsService : IICTEventPollerService
    {
        /// <summary>
        /// Delete old events.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task HardDeleteOldEvents();
    }

    /// <summary>
    /// Interface for AzureTableStorage.
    /// </summary>
    public interface IICTEventPollerCheckPointAzureTableStorageService : IICTEventPollerService
    {
        /// <summary>
        /// Delete old events.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task HardDeleteOldEvents();
    }
}
