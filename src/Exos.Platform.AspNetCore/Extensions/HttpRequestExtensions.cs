namespace Exos.Platform.AspNetCore.Extensions
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Extension methods for the HttpRequest class.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Returns the value from the HTTP Request Header Tracking-Id.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>Value from the HTTP Request Header Tracking-Id.</returns>
        public static string GetTrackingId(this HttpRequest request)
        {
            if (request != null && request.Headers != null && request.Headers.TryGetValue("Tracking-Id", out StringValues trackingId))
            {
                return trackingId;
            }

            return null;
        }

        /// <summary>
        /// Returns the value from the HTTP Request Header servicertenantidentifier (Servicer Tenant Id).
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>Value from the HTTP Request Header servicertenantidentifier.</returns>
        public static long? GetServicerContextTenantId(this HttpRequest request)
        {
            if (request != null && request.Headers != null &&
                request.Headers.TryGetValue("servicertenantidentifier", out StringValues servicerContextTenantId))
            {
                if (long.TryParse(servicerContextTenantId, out long servicerTenantId) && servicerTenantId > 0)
                {
                    return servicerTenantId;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the value from the HTTP Request Header workorderid (Work Order Id.).
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <returns>Value from the HTTP Request Header workorderid.</returns>
        public static long? GetWorkOrderId(this HttpRequest request)
        {
            if (request != null && request.Headers != null && request.Headers.TryGetValue("workorderid", out StringValues workorderIdInHeader))
            {
                if (long.TryParse(workorderIdInHeader, out long workorderId) && workorderId > 0)
                {
                    return workorderId;
                }
            }

            return null;
        }
    }
}
