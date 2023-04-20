#pragma warning disable CA1031 // Do not catch general exception types
namespace ListenerServiceSample.MessageProcessors
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Core.Listener;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Sample message processor.
    /// </summary>
    public class SampleProcessor : MessageProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SampleProcessor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleProcessor"/> class.
        /// </summary>
        /// <param name="serviceProvider">serviceProvider.</param>
        public SampleProcessor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = serviceProvider.GetService<ILogger<SampleProcessor>>();
        }

        /// <inheritdoc/>
        public override async Task<bool> Execute(string messageUtfText, MessageProperty messageProperty)
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                AzureMessageData inputRequest = JsonSerializer.Deserialize<AzureMessageData>(messageUtfText);
                if (inputRequest == null || string.IsNullOrEmpty(inputRequest.Message?.Payload))
                {
                    _logger.LogError("Data Trace Listener: Invalid message received");
                    throw new ArgumentException("Data Trace Listener: messageUtfText is not valid");
                }

                // inputRequest.Message.Payload
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception processing message.");
                return false;
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
