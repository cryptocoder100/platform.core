namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// An authorization handler that satisfies all authorization requirements for the purposes of testing.
    /// </summary>
    public class AllowAnonymousAuthorizationHandler : IAuthorizationHandler
    {
        /// <inheritdoc />
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));
            foreach (IAuthorizationRequirement requirement in context.PendingRequirements.ToList())
            {
                context.Succeed(requirement); // Simply pass all requirements
            }

            return Task.CompletedTask;
        }
    }
}

// References:
// https://stackoverflow.com/questions/41112564/asp-net-disable-authentication-in-development-environments