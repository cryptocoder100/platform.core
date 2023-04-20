namespace Exos.Platform.AspNetCore.Security
{
    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Configuration options for <see cref="ExosAuthTokenAuthenticationHandler" />.
    /// </summary>
    public class ExosAuthTokenAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the configuration used to connect to Redis..
        /// </summary>
        public string ReadWriteConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the TimeoutInMinutes
        /// Gets or sets timeout specified in minutes, 30days =  43200 minutes..
        /// </summary>
        public long TimeoutInMinutes { get; set; }
    }
}
