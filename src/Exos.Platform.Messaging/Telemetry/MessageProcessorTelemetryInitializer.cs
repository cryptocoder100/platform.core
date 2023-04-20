using System;
using Exos.Platform.Messaging.Core.Listener;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Exos.Platform.Messaging.Telemetry
{
    /// <summary>
    /// Includes the <see cref="MessageProcessor" /> name in telemetry.
    /// </summary>
    public class MessageProcessorTelemetryInitializer : ITelemetryInitializer
    {
        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            if (!(telemetry is RequestTelemetry requestTelemetry))
            {
                return;
            }

            if (telemetry is ISupportProperties propertiesAccessor)
            {
                // Replace the unhelpful, default operation name "Process" with the message processor name

                string messageProcessor;
                if (TryGetValueAndRemove(propertiesAccessor.Properties, "MessageProcessor", out messageProcessor) || TryGetValueAndRemove(propertiesAccessor.Properties, "Message.Processor", out messageProcessor))
                {
                    requestTelemetry.Name = messageProcessor;
                }
            }
        }

        private static bool TryGetValueAndRemove(IDictionary<string, string> properties, string key, out string value)
        {
            if (properties.TryGetValue(key, out value))
            {
                properties.Remove(key);
                return true;
            }

            return false;
        }
    }
}
