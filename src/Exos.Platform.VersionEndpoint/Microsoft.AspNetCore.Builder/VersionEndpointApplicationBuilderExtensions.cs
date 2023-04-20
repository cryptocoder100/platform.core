using System;
using Exos.Platform.BuildInformation;
using Exos.Platform.VersionEndpoint;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder" /> that enables a /version endpoint for displaying application information.
    /// </summary>
    public static class VersionEndpointApplicationBuilderExtensions
    {
        /// <summary>
        /// Enables a /version endpoint for returning application information.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder" /> instance.</param>
        public static IApplicationBuilder UseExosVersionEndpoint(this IApplicationBuilder app)
        {
            app.Map("/version", nestedApp =>
            {
                nestedApp.Run(context =>
                {
                    var buildInformation = nestedApp.ApplicationServices.GetService<IBuildInformation>();
                    if (buildInformation == null)
                    {
                        throw new InvalidOperationException("The IBuildInformation service must be registered. Call AddExosBuildInformation().");
                    }

                    var middleware = new VersionEndpointMiddleware(buildInformation);
                    return middleware.Invoke(context);
                });
            });

            return app;
        }
    }
}
