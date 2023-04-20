namespace Exos.Platform.Messaging.Helper
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exos Messaging Exception.
    /// </summary>
    public class ExosMessagingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessagingException"/> class.
        /// </summary>
        public ExosMessagingException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessagingException"/> class.
        /// </summary>
        /// <param name="message">Exception Message.</param>
        public ExosMessagingException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessagingException"/> class.
        /// </summary>
        /// <param name="message">Exception Message.</param>
        /// <param name="inner">Inner Exception.</param>
        public ExosMessagingException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessagingException"/> class.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        protected ExosMessagingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
        }

        /// <summary>
        /// Gets or sets ResourceReferenceProperty.
        /// </summary>
        public string ResourceReferenceProperty { get; set; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("ResourceReferenceProperty", ResourceReferenceProperty);
            base.GetObjectData(info, context);
        }
    }
}
