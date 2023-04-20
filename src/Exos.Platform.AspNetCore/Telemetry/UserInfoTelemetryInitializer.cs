using System;
using System.Collections.Generic;
using System.Text;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Security;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.AspNetCore.Telemetry
{
    internal sealed class UserInfoTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserInfoTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var supportProperties = telemetry as ISupportProperties;
            var context = _httpContextAccessor?.HttpContext;
            if (context != null && supportProperties != null)
            {
                var id = context?.User?.Identity?.GetUserId();
                if (!string.IsNullOrEmpty(id))
                {
                    supportProperties.Properties["User.ID"] = id;
                }

                var email = context.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(email))
                {
                    supportProperties.Properties["User.IsInternal"] = email.EndsWith("@svclnk.com", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
                }

                var isAdmin = context?.User?.Identity?.GetExosAdmin();
                if (isAdmin != null)
                {
                    supportProperties.Properties["User.IsAdmin"] = (bool)isAdmin ? "true" : "false";
                }
            }
        }
    }
}
