namespace Exos.Platform.Messaging.Core
{
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Defines the <see cref="FailoverOnExcessiveQueueExceptionPolicy"/>.
    /// </summary>
    public class FailoverOnExcessiveQueueExceptionPolicy : FailoverOnExcessiveExceptionPolicy<QueueClient>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverOnExcessiveQueueExceptionPolicy"/> class.
        /// </summary>
        /// <param name="config">The <see cref="FailoverConfig"/>.</param>
        public FailoverOnExcessiveQueueExceptionPolicy(FailoverConfig config) : base(config)
        {
        }
    }
}
