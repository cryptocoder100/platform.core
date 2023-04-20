namespace Exos.Platform.AspNetCore.Security
{
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// Empty/default implementation representing dynamic requirement coming from user context.
    /// </summary>
    public class UserContextRequirement : IAuthorizationRequirement
    {
    }
}
