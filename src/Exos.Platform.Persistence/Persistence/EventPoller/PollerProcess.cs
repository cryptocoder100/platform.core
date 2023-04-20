#pragma warning disable CA1028 // Enum Storage should be Int32
namespace Exos.Platform.Persistence.EventPoller
{
    /// <summary>
    /// Poller Process Type.
    /// </summary>
    public enum PollerProcess : byte
    {
        /// <summary>
        ///  None Poller Process.
        /// </summary>
        None,

        /// <summary>
        /// Represents a Service Bus Poller Process.
        /// </summary>
        ServiceBus = 1,

        /// <summary>
        /// Represents a Event Hub Poller Process.
        /// </summary>
        EventHub = 2,

        /// <summary>
        /// The integrations.
        /// </summary>
        Integrations = 3,

        /// <summary>
        /// AzureTableStorage.
        /// </summary>
        AzureTableStorage = 4
    }
}
#pragma warning restore CA1028 // Enum Storage should be Int32