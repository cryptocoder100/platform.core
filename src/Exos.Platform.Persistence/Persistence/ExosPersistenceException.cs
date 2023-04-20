namespace Exos.Platform.Persistence
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exos Persistence Exception.
    /// </summary>
    [Serializable]
    public class ExosPersistenceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExosPersistenceException"/> class.
        /// </summary>
        public ExosPersistenceException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosPersistenceException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ExosPersistenceException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosPersistenceException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException"><see cref="Exception"/>.</param>
        public ExosPersistenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosPersistenceException"/> class.
        /// </summary>
        /// <param name="serializationInfo"><see cref="SerializationInfo"/>.</param>
        /// <param name="streamingContext"><see cref="StreamingContext"/>.</param>
        protected ExosPersistenceException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}