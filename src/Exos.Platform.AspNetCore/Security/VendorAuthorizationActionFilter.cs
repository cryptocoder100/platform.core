namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Mvc.Filters;

    /// <summary>
    /// This Action filter will check for the vendor/subcontractor user to make sure that user
    /// will update only his own configuration.
    /// This filter will check for properties named vendorid/subcontractorid in the request payload.
    /// if found then check those values with user tenant id.
    /// </summary>
    public class VendorAuthorizationActionFilter : IActionFilter
    {
        private readonly IUserContextAccessor _userContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="VendorAuthorizationActionFilter"/> class.
        /// </summary>
        /// <param name="userContextAccessor">userContextAccessor.</param>
        public VendorAuthorizationActionFilter(IUserContextAccessor userContextAccessor)
        {
            _userContextAccessor = userContextAccessor ?? throw new ArgumentNullException(nameof(userContextAccessor));
        }

        /// <inheritdoc/>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do something after the action executes.
        }

        /// <inheritdoc/>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userContext = _userContextAccessor;
            if (userContext != null && (userContext.TenantType == TenantTypes.subcontractor.ToString() || userContext.TenantType == TenantTypes.vendor.ToString()))
            {
                if (context != null && context.ActionArguments != null && context.ActionArguments.Count > 0)
                {
                    long vendorId = GetVendorId(context.ActionArguments);
                    long subcontractorId = GetSubcontractorId(context.ActionArguments);
                    if (subcontractorId == 0)
                    {
                        subcontractorId = vendorId;
                    }

                    if (vendorId == 0)
                    {
                        vendorId = subcontractorId;
                    }

                    if (vendorId > 0 || subcontractorId > 0)
                    {
                        IsAuthorized(vendorId, subcontractorId, _userContextAccessor);
                    }
                }
            }
        }

        private static long GetSubcontractorId(IDictionary<string, object> arguments)
        {
            long subcontractorId = 0;
            if (arguments != null && arguments.Count > 0)
            {
                var argument = arguments.FirstOrDefault(arg => arg.Key.Equals("subcontractorid", StringComparison.OrdinalIgnoreCase));
                if (argument.Key != null)
                {
                    long.TryParse(argument.Value.ToString(), NumberStyles.Number, CultureInfo.CurrentCulture, result: out subcontractorId);
                }
                else
                {
                    subcontractorId = FindIdInObject(arguments, "subcontractorid");
                }
            }

            return subcontractorId;
        }

        private static long GetVendorId(IDictionary<string, object> arguments)
        {
            long vendorId = 0;
            if (arguments != null && arguments.Count > 0)
            {
                var argument = arguments.FirstOrDefault(arg => arg.Key.Equals("vendorid", StringComparison.OrdinalIgnoreCase));
                if (argument.Key != null)
                {
                    long.TryParse(argument.Value.ToString(), NumberStyles.Number, CultureInfo.CurrentCulture, result: out vendorId);
                }
                else
                {
                    vendorId = FindIdInObject(arguments, "vendorid");
                }
            }

            return vendorId;
        }

        private static long FindIdInObject(IDictionary<string, object> arguments, string fieldToFind)
        {
            long vendorId = 0;
            if (arguments != null && arguments.Count > 0)
            {
                var args = arguments.Where(argument => argument.Value != null && argument.Value.GetType() is object);
                if (args != null && args.Any())
                {
                    foreach (var arg in args)
                    {
                        var idproperty = arg.Value.GetType().GetProperties().FirstOrDefault(prop => prop.Name.Equals(fieldToFind, StringComparison.OrdinalIgnoreCase));
                        if (idproperty != null)
                        {
                            long.TryParse(idproperty.GetValue(arg.Value).ToString(), NumberStyles.Number, CultureInfo.CurrentCulture, result: out vendorId);
                            break;
                        }
                    }
                }
            }

            return vendorId;
        }

        private static void IsAuthorized(long vendorId, long subcontractorId, IUserContextAccessor userContextAccessor)
        {
            if (vendorId > 0 && userContextAccessor != null)
            {
                var userContext = userContextAccessor;
                if (userContext.TenantType == TenantTypes.subcontractor.ToString() || userContext.TenantType == TenantTypes.vendor.ToString())
                {
                    if (!(vendorId == userContext.VendorTenantId || subcontractorId == userContext.SubContractorTenantId || vendorId == userContext.TenantId || subcontractorId == userContext.TenantId))
                    {
                        throw new UnauthorizedException("Logged-in user is not authorized to perform this operation.");
                    }
                }
            }
        }
    }
}