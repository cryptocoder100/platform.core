namespace Exos.Platform.Persistence.EventTracking
{
    using System;

    /// <summary>
    /// Future Event Context.
    /// </summary>
    public class FutureEventCtx
    {
        /// <summary>
        /// Gets or sets a value indicating whether Future Event Generation is disabled.
        /// </summary>
        public bool DisableFutureEventGeneration { get; set; }

        /// <summary>
        /// Gets or sets payload which gets to the event.
        /// </summary>
        public string FutureEventPayload { get; set; }

        /// <summary>
        /// Gets or sets the Due Date of the future event.
        /// </summary>
        public object FutureEventDueDataDataCtx { get; set; }
    }
}
