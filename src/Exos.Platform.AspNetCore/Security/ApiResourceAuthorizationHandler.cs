// https://stackoverflow.com/questions/41533910/how-create-custom-authorization-attribute-for-checking-a-role-and-url-path-in-as
// https://stackoverflow.com/questions/36445780/how-to-implement-permission-based-access-control-with-asp-net-core/36447358#36447358
// https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies
namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Implementation for AuthorizationHandler UserContextRequirement.
    /// </summary>
    public class ApiResourceAuthorizationHandler : AuthorizationHandler<UserContextRequirement>
    {
        private readonly IOptions<PlatformDefaultsOptions> _options;
        private readonly ILogger<ApiResourceAuthorizationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResourceAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="options">PlatformDefaultsOptions.</param>
        /// <param name="logger">Logger instance.</param>
        public ApiResourceAuthorizationHandler(IOptions<PlatformDefaultsOptions> options, ILogger<ApiResourceAuthorizationHandler> logger)
        {
            _options = options;
            _logger = logger ?? new NullLogger<ApiResourceAuthorizationHandler>();
        }

        /// <inheritdoc/>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserContextRequirement requirement)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.LogDebug($"AuthorizationByConvention Options Value {_options.Value.AuthorizationByConvention}");
            if (!_options.Value.AuthorizationByConvention)
            {
                // When we're disabled, behave as if requirement is met
                context.Succeed(requirement);
            }
            else
            {
                var user = context.User;
                var filterContext = context.Resource as AuthorizationFilterContext;

                if (user?.Identity != null && filterContext != null)
                {
                    var applicationName = AssemblyHelper.EntryAssemblyName;
                    var controllerName = $"{applicationName}.{filterContext.RouteData.Values["controller"] as string}";
                    var actionName = $"{controllerName}.{filterContext.RouteData.Values["action"] as string}";
                    var apiResource = $"{controllerName}.{filterContext.HttpContext.Request.Method.ToUpperInvariant()}.{filterContext.ActionDescriptor.AttributeRouteInfo.Template}";

                    var hasElevatedRight = CheckForElevatedRight(filterContext, user);
                    if (hasElevatedRight)
                    {
                        context.Succeed(requirement);
                        _logger.LogTrace("User '{User}' is authorized by ElevatedRight to access '{Resource}'.", user?.Identity?.Name, apiResource);
                        return Task.CompletedTask;
                    }

                    _logger.LogTrace("Looking for '{ApplicationName}' OR '{ControllerName}' OR '{ActionName}' OR '{ApiResource}' user role (API resource) for '{User}'...", applicationName, controllerName, actionName, apiResource, user?.Identity?.Name);

                    var matchingClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role &&
                                                                (applicationName.Equals(c.Value, StringComparison.OrdinalIgnoreCase) ||
                                                                 controllerName.Equals(c.Value, StringComparison.OrdinalIgnoreCase) ||
                                                                 actionName.Equals(c.Value, StringComparison.OrdinalIgnoreCase) ||
                                                                 apiResource.Equals(c.Value, StringComparison.OrdinalIgnoreCase)));
                    if (matchingClaim != null)
                    {
                        // Requirement has been met
                        context.Succeed(requirement);
                        _logger.LogTrace("User '{User}' is authorized by convention to access '{Claim}'.", user?.Identity?.Name, matchingClaim?.Value);
                    }
                    else
                    {
                        // https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies#what-should-a-handler-return
                        context.Fail();
                        _logger.LogWarning("User '{User}' does not have authorization by convention to access '{ApplicationName}' OR '{ControllerName}' OR '{ActionName}' OR '{ApiResource}'.", user?.Identity?.Name, applicationName, controllerName, actionName, apiResource);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static bool CheckForElevatedRight(AuthorizationFilterContext context, ClaimsPrincipal user)
        {
            const string elevatedRightClaimName = "ElevatedRight";
            var hasRight = context.HttpContext.Request.Headers.ContainsKey(elevatedRightClaimName);
            if (hasRight == false)
            {
                return false;
            }

            var rightValue = context.HttpContext.Request.Headers[elevatedRightClaimName];

            var claimToCheck = user.Claims.FirstOrDefault(c => c.Type == ExosClaimTypes.ClaimsSignature);

            if (claimToCheck == null)
            {
                return false;
            }

            return rightValue == claimToCheck.Value;
        }
    }
}