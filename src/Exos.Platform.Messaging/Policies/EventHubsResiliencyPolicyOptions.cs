namespace Exos.Platform.Messaging.Policies
{
    /// <summary>
    /// EventHubsResiliencyPolicyOptions.
    /// </summary>
    public class EventHubsResiliencyPolicyOptions
    {
        /// <summary>
        /// Gets or sets MaxRetries.
        /// </summary>
        public int MaxRetries { get; set; } = 30; // One can always overide this by having this come from one's svc app Settigns.
    }
}
