namespace Exos.Platform.Messaging.Core.StreamListener
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// ExosStreamProcessor.
    /// </summary>
    public abstract class ExosStreamProcessor
    {
        /// <summary>
        /// Gets or sets MessageConfigurationSection.
        /// </summary>
        public MessageSection MessageConfigurationSection { get; set; }

        /// <summary>
        /// Execute method.
        /// </summary>
        /// <param name="streamToProcess">streamToProcess.</param>
        /// <returns>task.</returns>
        public abstract Task<List<ExosFailedStream>> Execute(List<ExosStream> streamToProcess);
    }
}
