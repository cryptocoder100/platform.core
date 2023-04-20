namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Provides extensions for registering MethodOverrideMiddleware.
    /// </summary>
    public static class MethodOverrideExtensions
    {
        /// <summary>
        /// Adds the MethodOverrideMiddleware to the pipeline that will utilize the Http-Method-Override request header
        /// to override the method specified in the request.
        /// </summary>
        /// <param name="builder">IApplicationBuilder.</param>
        /// <returns>An configured instance of <see cref="IApplicationBuilder"/> with the ValidateAntiforgeryTokenMiddleware.</returns>
        [Obsolete("Call UseHttpMethodOverride already supplied by ASP.NET instead")]
        public static IApplicationBuilder UseMethodOverride(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<MethodOverrideMiddleware>();
        }
    }
}
