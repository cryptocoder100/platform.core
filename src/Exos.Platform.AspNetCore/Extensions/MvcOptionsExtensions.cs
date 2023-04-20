namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Authorization;

    /// <summary>
    /// Extension methods for the MvcOptions class.
    /// </summary>
    public static class MvcOptionsExtensions
    {
        /// <summary>
        /// Adjusts the default MVC configuration to be consistent with platform defaults with regard to authorization requirements.
        /// </summary>
        /// <param name="options">The MvcOptions to configure.</param>
        /// <returns>The updated MvcOptions.</returns>
        public static MvcOptions AddPlatformDefaults(this MvcOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Secure all controllers and actions by default.
            // To allow anonymous access, use the AllowAnonymous attribute.
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new UserContextRequirement())
                .Build();

            options.Filters.Add(new AuthorizeFilter(policy));

            // TODO Force SSL in production
            // config.Filters.Add(new RequireHttpsAttribute());
            return options;
        }
    }
}
