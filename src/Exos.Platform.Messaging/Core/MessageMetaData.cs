namespace Exos.Platform.Messaging.Core
{
    /// <summary>
    /// Message MetaData.
    /// </summary>
    public class MessageMetaData
    {
        /// <summary>
        /// This value in the MessageMetaData field is in UTC.The scheduled enqueue time in UTC.
        /// This value is for delayed message sending. Delay messages sending to a specific time in the future.
        /// Value is in seconds.
        /// </summary>
        public const string ScheduleEnqueueDelayTime = "ScheduleEnqueueDelayTime";

        /// <summary>
        /// Message TimeToLive.
        /// </summary>
        public const string TimeToLive = "TimeToLive";

        /// <summary>
        /// Message TimeToLive Default.
        /// </summary>
        public const double TimeToLiveDefault = 5;

        /// <summary>
        /// PartitionKey.
        /// </summary>
        public const string PartitionKey = "PartitionKey";

        /// <summary>
        /// CorrelationId.
        /// </summary>
        public const string CorrelationId = "CorrelationId";

        /// <summary>
        /// MessageGuid.
        /// </summary>
        public const string MessageGuid = "MessageGuid";

        /// <summary>
        /// Gets or sets DataFieldName.
        /// </summary>
        public string DataFieldName { get; set; }

        /// <summary>
        /// Gets or sets DataFieldValue.
        /// </summary>
        public string DataFieldValue { get; set; }
    }
}
