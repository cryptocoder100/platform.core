namespace Exos.Platform.AspNetCore.Security
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// An implementation of the <see cref="IUserAccessor" /> interface to provider access to the current request user.
    /// </summary>
    public class UserAccessor : IUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAccessor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor.</param>
        public UserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the ClaimsPrincipal of the current request user.
        /// </summary>
        public ClaimsPrincipal CurrentUser
        {
            get
            {
                var user = _httpContextAccessor?.HttpContext?.User;
                return user;
            }
        }
    }
}
