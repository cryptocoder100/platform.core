namespace Exos.Platform.Messaging.Helper
{
    /// <summary>
    /// Constant values.
    /// </summary>
    public static class ExosMessagingConstant
    {
        /// <summary>
        /// Configuration is null.
        /// </summary>
        public const string NullConfiguration = "Configuration is null.";

        /// <summary>
        /// Message data is null.
        /// </summary>
        public const string NullMessageData = "Message data is null.";

        /// <summary>
        /// Message Object is null.
        /// </summary>
        public const string MessageObjectNull = "Message Object is null";

        /// <summary>
        /// Message entity not found.
        /// </summary>
        public const string MessageEntityNotFound = "Message entity not found";

        /// <summary>
        /// Argument Null.
        /// </summary>
        public const string ArgumentNull = "ArgumentNull";

        /// <summary>
        /// ACTIVE status.
        /// </summary>
        public const string AzureEntityStatusActive = "ACTIVE";

        /// <summary>
        /// FAILED status.
        /// </summary>
        public const string AzureMessagePublishStatusFailed = "FAILED";

        /// <summary>
        /// SUCCEEDED status indicating message retry succeeded.
        /// </summary>
        public const string AzureMessagePublishStatusSucceeded = "SUCCEEDED";

        /// <summary>
        /// Queue.
        /// </summary>
        public const string AzureMessageEntityQueue = "Queue";

        /// <summary>
        /// Topic.
        /// </summary>
        public const string AzureMessageEntityTopic = "Topic";

        /// <summary>
        /// Retry Count.
        /// </summary>
        public const int RetryCount = 5;

        /// <summary>
        /// Number Of Threads.
        /// </summary>
        public const int NumberOfThreads = 5;

        /// <summary>
        /// Success Code.
        /// </summary>
        public const int SuccessCode = 9999;

        /// <summary>
        /// Write Failed Code.
        /// </summary>
        public const int WriteFailedCode = 9001;

        /// <summary>
        /// Write to DB Failed Code.
        /// </summary>
        public const int WriteToDbFailedCode = 9002;
    }
}
