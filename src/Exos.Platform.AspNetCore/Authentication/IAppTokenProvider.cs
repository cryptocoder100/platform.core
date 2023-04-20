namespace Exos.Platform.AspNetCore.Authentication
{
    using System.Threading.Tasks;

    /// <summary>
    /// App token provider interface.
    /// Implement this for implementing tokens from different idp.
    /// </summary>
    public interface IAppTokenProvider
    {
        /// <summary>
        /// Method for generating access token.
        /// </summary>
        /// <param name="clientId">userid/clientid.</param>
        /// <param name="clientSecret">Secret/password.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<string> GetToken(string clientId, string clientSecret);

        /// <summary>
        /// Method for generating access token.
        /// </summary>
        /// <param name="clientId">userid/clientid.</param>
        /// <param name="clientSecret">Secret/password.</param>
        /// <param name="generateNewToken">pass parameter to force the generate token.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<string> GetToken(string clientId, string clientSecret, bool generateNewToken);

        /// <summary>
        /// Gets a secure string allowing validation of additional user context.
        /// </summary>
        /// <param name="userName">Username being validated.</param>
        /// <param name="email">Email being validated.</param>
        /// <param name="expirationTicks">Expiration ticks.</param>
        /// <returns>
        /// An additional user context to be passed by the client, along with appropriate token.
        /// </returns>
        string GetAdditionalUserContext(string userName, string email, string expirationTicks);
    }
}
