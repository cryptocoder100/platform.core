namespace Exos.Platform.Messaging.Core
{
    /// <summary>
    /// Events hubs config section.
    /// </summary>
    public class EventingSection : MessageSection
    {
        /// <summary>
        /// Gets or sets max batch size.
        /// </summary>
        public int MaxBatchSize { get; set; } = 100; // Default to 100
    }
}
