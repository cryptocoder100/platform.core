namespace Exos.Platform.AspNetCore.Security
{
    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Configuration options for <see cref="ApiKeyAuthenticationHandler" />.
    /// </summary>
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Gets or sets the regular expression pattern to match against the API key.
        /// </summary>
        public string ApiKeyPattern { get; set; }

        /// <summary>
        /// Gets or sets the Authorization header scheme the API key will use.
        /// </summary>
        public string AuthorizationScheme { get; set; }

        /// <summary>
        /// Gets or sets the Redis configuration string.
        /// </summary>
        public string RedisConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the UserSvc endpoint.
        /// </summary>
        public string UserSvc { get; set; }

        /// <summary>
        /// Gets or sets the time in seconds for caching API key lookups.
        /// </summary>
        public int? CacheDuration { get; set; }
    }
}
