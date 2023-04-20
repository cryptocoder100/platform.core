namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for enabling <see cref="ExosAuthTokenAuthenticationHandler" />.
    /// </summary>
    public static class ExosAuthTokenExtensions
    {
        /// <summary>
        /// Registers the EXOS authtoken-based authentication handler.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>AuthenticationBuilder.</returns>
        public static AuthenticationBuilder AddExosAuthToken(this AuthenticationBuilder builder)
            => builder.AddExosAuthToken(configureOptions: null);

        /// <summary>
        /// Registers the EXOS authtoken-based authentication handler.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="configureOptions">The configure options callback.</param>
        /// <returns>AuthenticationBuilder.</returns>
        public static AuthenticationBuilder AddExosAuthToken(this AuthenticationBuilder builder, Action<ExosAuthTokenAuthenticationOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Add(ServiceDescriptor.Singleton<IPostConfigureOptions<ExosAuthTokenAuthenticationOptions>, PostConfigureExosAuthTokenAuthenticationOptions>());
            return builder.AddScheme<ExosAuthTokenAuthenticationOptions, ExosAuthTokenAuthenticationHandler>(ExosAuthTokenAuthenticationDefaults.AuthenticationScheme, displayName: null, configureOptions: configureOptions);
        }
    }
}
