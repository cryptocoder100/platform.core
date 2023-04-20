namespace Exos.Platform.Messaging.Core.StreamListener
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs.Processor;

    /// <summary>
    /// Implement a listener to register with event hub streaming.
    /// </summary>
    public interface IExosStreamingListener
    {
        /// <summary>
        /// Registers the Listeners/EventProcessors.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        /// <param name="onRegistrationError">Error on Registration.</param>
        /// <returns>List of <see cref="EventProcessorHost"/>.</returns>
        Task<List<EventProcessorHost>> RegisterListener(IServiceProvider serviceProvider, Action<string, Exception> onRegistrationError);
    }
}