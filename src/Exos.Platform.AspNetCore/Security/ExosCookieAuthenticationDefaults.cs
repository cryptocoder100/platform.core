namespace Exos.Platform.AspNetCore.Security
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Default values related to the <see cref="ExosCookieAuthenticationHandler" />.
    /// </summary>
    public static class ExosCookieAuthenticationDefaults
    {
        /// <summary>
        /// The EXOS cookie-based authentication scheme..
        /// </summary>
        public static readonly string AuthenticationScheme = PlatformAuthScheme.ExosCookies.ToString();
    }
}
