namespace Exos.Platform.AspNetCore.Middleware
{
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Provides extensions for registering ValidateAntiforgeryTokenMiddleware.
    /// </summary>
    public static class ValidateAntiforgeryTokenExtensions
    {
        /// <summary>
        /// Adds the ValidateAntiforgeryTokenMiddleware to the pipeline.
        /// </summary>
        /// <param name="builder">IApplicationBuilder.</param>
        /// <returns>An configured instance of <see cref="IApplicationBuilder"/> with the ValidateAntiforgeryTokenMiddleware.</returns>
        public static IApplicationBuilder UseValidateAntiForgeryToken(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidateAntiforgeryTokenMiddleware>();
        }
    }
}
