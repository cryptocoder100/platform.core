#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.ICTLibrary.Core.Model
{
    using System.Collections.Generic;
    using Exos.Platform.TenancyHelper.MultiTenancy;

    /// <summary>
    /// Target Messaging Platform Enum.
    /// </summary>
    public enum TargetMessagingPlatform
    {
        /// <summary>
        /// Represents Service Bus.
        /// </summary>
        ServiceBus,

        /// <summary>
        /// Represents Event Hub.
        /// </summary>
        EventHub,
    }

    /// <summary>
    /// ICT Event Message.
    /// </summary>
    public class IctEventMessage
    {
        /// <summary>
        /// Gets or Sets the UserContext which is publishing the message.
        /// </summary>
        public UserContext UserContext { get; set; }

        /// <summary>
        /// Gets or Sets the Tracking id from the publishing application.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or Sets the priority initially this will not be implemented.
        /// </summary>
        public short Priority { get; set; }

        /// <summary>
        /// Gets or Sets the EventName associated with the message.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or Sets the Entity name where the event is associated.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or Sets the Publisher application/Micro service name.
        /// </summary>
        public string PublisherName { get; set; }

        /// <summary>
        /// Gets or Sets the Service name's id which is stored in ICT.
        /// Use application name instead of id.
        /// </summary>
        public short PublisherId { get; set; }

        /// <summary>
        /// Gets or Sets any additional key values need to be part of the header message.
        /// This will go to the message header and can be used to filter the messages.
        /// </summary>
        public List<KeyValuePair<string, string>> AdditionalMessageHeaderData { get; set; }

        /// <summary>
        /// Gets or Sets the Payload string.
        /// </summary>
        public string Payload { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only