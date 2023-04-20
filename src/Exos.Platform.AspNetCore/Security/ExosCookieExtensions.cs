namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for enabling <see cref="ExosCookieAuthenticationHandler" />.
    /// </summary>
    public static class ExosCookieExtensions
    {
        /// <summary>
        /// Registers the EXOS cookie-based authentication handler.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>AuthenticationBuilder.</returns>
        public static AuthenticationBuilder AddExosCookie(this AuthenticationBuilder builder)
            => builder.AddExosCookie(configureOptions: null);

        /// <summary>
        /// Registers the EXOS cookie-based authentication handler.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="configureOptions">The configure options callback.</param>
        /// <returns>AuthenticationBuilder.</returns>
        public static AuthenticationBuilder AddExosCookie(this AuthenticationBuilder builder, Action<ExosCookieAuthenticationOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Add(ServiceDescriptor.Singleton<IPostConfigureOptions<ExosCookieAuthenticationOptions>, PostConfigureExosCookieAuthenticationOptions>());
            return builder.AddScheme<ExosCookieAuthenticationOptions, ExosCookieAuthenticationHandler>(ExosCookieAuthenticationDefaults.AuthenticationScheme, displayName: null, configureOptions: configureOptions);
        }
    }
}
