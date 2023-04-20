namespace Exos.Platform.Persistence.Persistence
{
    using System;

    /// <summary>
    /// Thrown if a blobstore is found in the database but not configured in config file.
    /// </summary>
    public class ServiceNotProperlyConfiguredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNotProperlyConfiguredException"/> class.
        /// </summary>
        /// <param name="message">message.</param>
        public ServiceNotProperlyConfiguredException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNotProperlyConfiguredException"/> class.
        /// </summary>
        public ServiceNotProperlyConfiguredException() : base(GetMessage())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNotProperlyConfiguredException"/> class.
        /// </summary>
        /// <param name="message">message.</param>
        /// <param name="innerException">inner exception.</param>
        public ServiceNotProperlyConfiguredException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private static string GetMessage()
        {
            return "The configuration is not complete.  In order to use this class you must add 'services.AddSingleton<IConfiguration>(configuration);' to your startup, placing the original startup configuration into the DI system to be injected into this class";
        }
    }
}