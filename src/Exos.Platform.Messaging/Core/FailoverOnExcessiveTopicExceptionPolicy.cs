namespace Exos.Platform.Messaging.Core
{
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Defines the <see cref="FailoverOnExcessiveTopicExceptionPolicy"/>.
    /// </summary>
    public class FailoverOnExcessiveTopicExceptionPolicy : FailoverOnExcessiveExceptionPolicy<TopicClient>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverOnExcessiveTopicExceptionPolicy"/> class.
        /// </summary>
        /// <param name="config">The <see cref="FailoverConfig"/>.</param>
        public FailoverOnExcessiveTopicExceptionPolicy(FailoverConfig config) : base(config)
        {
        }
    }
}