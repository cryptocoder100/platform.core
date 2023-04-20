#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.Messaging.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// Message Data.
    /// </summary>
    public class MessageData
    {
        /// <summary>
        /// Gets or Sets Unique id from the caller trace or group the messages when listening.
        /// </summary>
        public string PublisherMessageUniqueId { get; set; }

        /// <summary>
        /// Gets or Sets Additional metadata where subscription can apply filter on.
        /// </summary>
        public IList<MessageMetaData> AdditionalMetaData { get; set; }

        /// <summary>
        /// Gets or Sets serialized payload string.
        /// </summary>
        public string Payload { get; set; }
    }

    // <summary>
    // NOT USED . We will use the priority type if we have more priorities
    // </summary>
    // public enum PriorityType
    // {
    //    Normal = 0,
    //    /// <summary>
    //    /// Propose some other action.
    //    /// </summary>
    //    [Display(Name = "Medium", Description = "Medium Priority.")]
    //    Medium = 1,
    //    /// <summary>
    //    ///  high priority message.
    //    /// </summary>
    //    [Display(Name = "High", Description = "High Priority")]
    //    High = 2
    // }
}
#pragma warning restore CA2227 // Collection properties should be read only