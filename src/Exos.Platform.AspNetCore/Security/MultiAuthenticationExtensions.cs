namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Options;

    // https://github.com/aspnet/Security/blob/rel/2.0.0/src/Microsoft.AspNetCore.Authentication/AuthAppBuilderExtensions.cs

    /// <summary>
    /// Extension methods to add authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class MultiAuthenticationExtensions
    {
        /// <summary>
        /// Adds the <see cref="MultiAuthenticationMiddleware" /> to the specified IApplicationBuilder, which enables authentication capabilities.
        /// </summary>
        /// <param name="app">The IApplicationBuilder to add the middleware to.</param>
        /// <param name="schemes">A list of schemes to use for authentication.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseAuthentication(this IApplicationBuilder app, params string[] schemes)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (schemes == null || schemes.Length == 0)
            {
                throw new ArgumentNullException(nameof(schemes));
            }

            var options = new MultiAuthenticationOptions();
            options.Schemes.AddRange(schemes);

            return app.UseMiddleware<MultiAuthenticationMiddleware>(Options.Create(options));
        }
    }
}
