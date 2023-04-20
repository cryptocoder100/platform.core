namespace Exos.Platform.AspNetCore.Middleware
{
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Provides extensions for registering QueryStringAuthMiddleware.
    /// </summary>
    public static class QueryStringAuthExtensions
    {
        /// <summary>
        /// Adds the QueryStringAuthMiddleware to the pipeline.
        /// </summary>
        /// <param name="builder">IApplicationBuilder.</param>
        /// <returns>An configured instance of <see cref="IApplicationBuilder"/> with the QueryStringAuthMiddleware.</returns>
        public static IApplicationBuilder UseQueryStringAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<QueryStringAuthMiddleware>();
        }
    }
}
