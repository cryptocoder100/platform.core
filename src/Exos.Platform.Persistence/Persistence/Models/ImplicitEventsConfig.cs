namespace Exos.Platform.Persistence.Models
{
    /// <summary>
    /// Implicit Events Configuration.
    /// </summary>
    public class ImplicitEventsConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether PublishConsolidatedPayload.
        /// </summary>
        public bool PublishConsolidatedPayload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether PublishCurrentState.
        /// </summary>
        public bool PublishCurrentState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DefaultDueDateToCurrentTimeStamp.
        /// </summary>
        public bool DefaultDueDateToCurrentTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether TryGetSingleKeyValue.
        /// </summary>
        public bool TryGetSingleKeyValue { get; set; }
    }
}
