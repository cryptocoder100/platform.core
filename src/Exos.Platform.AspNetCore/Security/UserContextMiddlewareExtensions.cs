namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using Exos.Platform.AspNetCore.Middleware;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides extension methods for the <see cref="UserContextMiddleware" />.
    /// </summary>
    public static class UserContextMiddlewareExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline and set the current context principal.
        /// </summary>
        /// <param name="builder">IApplicationBuilder.</param>
        /// <param name="additionalAuthSchemes">List of names of auth schemes that are added to UseAuthentiction call.</param>
        /// <returns>An configured instance of <see cref="IApplicationBuilder"/> with the UserContextMiddleware.</returns>
        public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder, IEnumerable<string> additionalAuthSchemes = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configuration = builder.ApplicationServices.GetService<IConfiguration>();
            if (configuration.GetValue<bool>("OAuth:Enabled", false))
            {
                var oauthSchemes = configuration.GetSection("OAuth:AuthSchemes").Get<List<string>>();

                if (additionalAuthSchemes != null)
                {
                    oauthSchemes.AddRange(additionalAuthSchemes);
                }

                builder.UseAuthentication(oauthSchemes.ToArray());
                builder.UseAuthorization();
            }

            // Build user context "claims" after successful auth
            builder.UseMiddleware<UserContextMiddleware>();
            builder.UseMiddleware<ExosImpersonationMiddleware>();

            return builder;
        }
    }
}
