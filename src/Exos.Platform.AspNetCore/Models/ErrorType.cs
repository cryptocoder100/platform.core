namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// Specifies the type of error in an <see cref="ErrorModel" />.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Failed to connect to API.
        /// </summary>
        ApiConnectionError,

        /// <summary>
        /// A general API error.
        /// </summary>
        ApiError,

        /// <summary>
        /// Caller failed to properly authenticate themselves in the request.
        /// </summary>
        AuthenticationError,

        /// <summary>
        /// The request contained invalid parameters.
        /// </summary>
        InvalidRequestError,

        /// <summary>
        /// The caller has made too many API requests for a given period of time.
        /// </summary>
        RateLimitError,
    }
}
