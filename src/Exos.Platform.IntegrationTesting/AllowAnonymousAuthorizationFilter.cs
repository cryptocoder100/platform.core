using Microsoft.AspNetCore.Authorization;

namespace Exos.Platform.IntegrationTesting
{
    /// <inheritdoc/>
    public class AllowAnonymousAuthorizationFilter : IAuthorizationHandler
    {
        /// <inheritdoc/>
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (IAuthorizationRequirement requirement in context.PendingRequirements.ToList())
            {
                context.Succeed(requirement); // Simply pass all requirements
            }

            return Task.CompletedTask;
        }
    }
}

// References:
// https://stackoverflow.com/questions/41112564/asp-net-disable-authentication-in-development-environment
