namespace Exos.Platform.AspNetCore.Middleware
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Anti-forgery service interface.
    /// Used for csrf validation.
    /// </summary>
    public interface IAntiforgeryService
    {
        /// <summary>
        /// Generates the token based on the input.
        /// </summary>
        /// <param name="userInfo">User Info.</param>
        /// <returns>Encoded and Encrypted user info.</returns>
        string GenerateCsrfToken(string userInfo);

        /// <summary>
        /// Validates/enforces the csrf headers.
        /// </summary>
        /// <param name="httpContext">HttpContext.</param>
        void ValidateRequestAsync(HttpContext httpContext);
    }
}
