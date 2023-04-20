namespace Exos.Platform.AspNetCore.Security
{
    using System.Security.Claims;

    /// <summary>
    /// An abstraction for providing access to the current request user.
    /// </summary>
    public interface IUserAccessor
    {
        /// <summary>
        /// Gets the ClaimsPrincipal of the current request user..
        /// </summary>
        ClaimsPrincipal CurrentUser { get; }
    }
}
