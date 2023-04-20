namespace Exos.Platform.AspNetCore.Security
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Default values related to the <see cref="ApiKeyAuthenticationHandler" />.
    /// </summary>
    public static class ApiKeyAuthenticationDefaults
    {
        /// <summary>
        /// The default Authorization header scheme.
        /// </summary>
        public const string AuthorizationScheme = "Basic";

        /// <summary>
        /// The default API key pattern.
        /// </summary>
        public const string ApiKeyPattern = "^[A-Za-z0-9+/]+={0,2}$";

        /// <summary>
        /// The default cache duration.
        /// </summary>
        public const int CacheDuration = 1800;

        /// <summary>
        /// The API key authentication scheme.
        /// </summary>
        public static readonly string AuthenticationScheme = PlatformAuthScheme.ApiKey.ToString();
    }
}
