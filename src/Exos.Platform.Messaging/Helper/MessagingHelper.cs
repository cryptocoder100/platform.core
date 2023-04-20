#pragma warning disable CA1031 // Do not catch general exception types

namespace Exos.Platform.Messaging.Helper
{
    using System;
    using System.Diagnostics;
    using System.Text.Json;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Core;
    using Microsoft.Azure.ServiceBus;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the <see cref="MessagingHelper"/>.
    /// This class is for shared static helper methods.
    /// </summary>
    public static class MessagingHelper
    {
        private static JsonSerializerOptions _azureDataMessageSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Check if specified connection strings point to same namespace.
        /// </summary>
        /// <param name="firstConnectionString">The first connection string.</param>
        /// <param name="secondConnectionString">The second connection string.</param>
        /// <returns>True if same namepace.</returns>
        public static bool AreSameNamespaces(string firstConnectionString, string secondConnectionString)
        {
            int? index = firstConnectionString?.IndexOf(';', StringComparison.Ordinal);

            if (index == null)
            {
                return secondConnectionString == null;
            }

            if (index.Value <= 0)
            {
                return secondConnectionString != null
                    && (index.Value == 0
                    ? secondConnectionString.Length == 0
                    : secondConnectionString.StartsWith(firstConnectionString, StringComparison.OrdinalIgnoreCase));
            }

            if (secondConnectionString == null
                || index.Value >= secondConnectionString.Length
                || secondConnectionString[index.Value] != ';')
            {
                return false;
            }

            return secondConnectionString.StartsWith(
                firstConnectionString.Substring(0, index.Value),
                StringComparison.OrdinalIgnoreCase);
        }

        internal static void TryEnrichActivity(Activity activity, byte[] messageBody)
        {
            if (activity == null || messageBody == null)
            {
                return;
            }

            // Attempt to read the bytes as an AzureMessageData JSON stream. That is the object
            // format MOST of our services use so it's a pretty safe bet for getting useful telemetry.

            try
            {
                var azureMessageData = System.Text.Json.JsonSerializer.Deserialize(messageBody, typeof(AzureMessageData), _azureDataMessageSerializerOptions) as AzureMessageData;
                if (azureMessageData != null)
                {
                    if (!string.IsNullOrEmpty(azureMessageData.TopicName))
                    {
                        TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("Message.Topic", azureMessageData.TopicName));
                    }

                    if (azureMessageData.MessageGuid != Guid.Empty)
                    {
                        TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("Message.ID", azureMessageData.MessageGuid.ToString("D")));
                    }

                    var messageType = azureMessageData.Message?.AdditionalMetaData?.FirstOrDefault(d => d.DataFieldName == "MessageType" && !string.IsNullOrEmpty(d.DataFieldValue));
                    if (messageType != null)
                    {
                        TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("Message.Type", messageType.DataFieldValue));
                    }

                    var eventName = azureMessageData.Message?.AdditionalMetaData?.FirstOrDefault(d => d.DataFieldName == "EventName" && !string.IsNullOrEmpty(d.DataFieldValue));
                    if (eventName != null)
                    {
                        TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("Message.EventName", eventName.DataFieldValue));
                    }

                    var @event = azureMessageData.Message?.AdditionalMetaData?.FirstOrDefault(d => d.DataFieldName == "Event" && !string.IsNullOrEmpty(d.DataFieldValue));
                    if (@event != null)
                    {
                        TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("Message.Event", @event.DataFieldValue));
                    }

                    if (!string.IsNullOrEmpty(azureMessageData.Message?.Payload))
                    {
                        // We are using Newtonsoft to be as forgiving to formatting as possible
                        // e.g. deserialize numbers as strings.
                        var payload = JsonConvert.DeserializeObject<PayloadCommon>(azureMessageData.Message.Payload);
                        if (!string.IsNullOrEmpty(payload?.OrderId))
                        {
                            TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("OrderId", payload.OrderId));
                        }

                        if (!string.IsNullOrEmpty(payload?.WorkOrderId))
                        {
                            TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("WorkOrderId", payload.WorkOrderId));
                        }

                        if (!string.IsNullOrEmpty(payload?.TrackingId))
                        {
                            TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("TrackingId", payload.TrackingId));
                        }

                        if (!string.IsNullOrEmpty(payload?.ServicerId))
                        {
                            TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("ServicerTenantId", payload.ServicerId));
                        }

                        if (!string.IsNullOrEmpty(payload?.ServicerTenantId))
                        {
                            TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("ServicerTenantId", payload.ServicerTenantId));
                        }

                        if (!string.IsNullOrEmpty(payload?.ExosServicerId))
                        {
                            TelemetryHelper.TryEnrichActivity(activity, KeyValuePair.Create("ServicerTenantId", payload.ExosServicerId));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Okay to swallow logging failures
                Debug.WriteLine(ex);
            }
        }

        // What we hope could be common properties on message payloads.
        // This is only slightly better than guessing blind.
        private class PayloadCommon
        {
            public string OrderId { get; set; }

            public string WorkOrderId { get; set; }

            public string TrackingId { get; set; }

            public string ServicerId { get; set; }

            public string ServicerTenantId { get; set; }

            public string ExosServicerId { get; set; }
        }
    }
}
