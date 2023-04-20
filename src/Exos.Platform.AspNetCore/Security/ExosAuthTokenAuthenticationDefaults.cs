namespace Exos.Platform.AspNetCore.Security
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Default values related to the <see cref="ExosAuthTokenAuthenticationHandler" />.
    /// </summary>
    public static class ExosAuthTokenAuthenticationDefaults
    {
        /// <summary>
        /// The EXOS token authentication scheme.
        /// </summary>
        public static readonly string AuthenticationScheme = PlatformAuthScheme.ExosAuthToken.ToString();
    }
}
