namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Extension methods for enabling <see cref="ApiKeyAuthenticationHandler" />.
    /// </summary>
    public static class ApiKeyExtensions
    {
        /// <summary>
        /// Registers the <see cref="ApiKeyAuthenticationHandler" />.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>AuthenticationBuilder.</returns>
        public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder)
            => builder.AddApiKey(configureOptions: null);

        /// <summary>
        /// Registers the <see cref="ApiKeyAuthenticationHandler" />.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="configureOptions">The configure options callback.</param>
        /// <returns>AuthenticationBuilder.</returns>
        public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder, Action<ApiKeyAuthenticationOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.Add(ServiceDescriptor.Singleton<IPostConfigureOptions<ApiKeyAuthenticationOptions>, PostConfigureApiKeyAuthenticationOptions>());
            return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.AuthenticationScheme, displayName: null, configureOptions: configureOptions);
        }
    }
}
