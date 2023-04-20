namespace Exos.Platform.AspNetCore.Security
{
    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Contains the options used by the <see cref="ExosCookieAuthenticationHandler" />.
    /// </summary>
    public class ExosCookieAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the cookie name..
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the cookie validation key..
        /// </summary>
        public string ValidationKey { get; set; }

        /// <summary>
        /// Gets or sets the cookie decryption key..
        /// </summary>
        public string DecryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the cookie domain..
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to require SSL..
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// Gets or sets the TimeoutInMinutes
        /// Gets or sets timeout specified in minutes, 60..
        /// </summary>
        public long TimeoutInMinutes { get; set; }
    }
}
