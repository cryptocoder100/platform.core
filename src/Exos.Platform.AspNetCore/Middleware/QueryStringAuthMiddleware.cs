namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Convert incoming qs auth token to a Authorization header so the rest of the chain
    /// can authorize the request correctly
    /// authtoken and tracking-id are passed in query string from js .net core signal client.
    /// </summary>
    public class QueryStringAuthMiddleware
    {
        /// <summary>
        /// Defines the _next.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringAuthMiddleware"/> class.
        /// </summary>
        /// <param name="next">RequestDelegate next request.</param>
        public QueryStringAuthMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// Add Headers from the authorization token.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context != null)
            {
                if (context.Request.Query.TryGetValue("oauth_token", out var oauthToken))
                {
                    context.Request.Headers.Add("Authorization", "Bearer " + oauthToken.First());
                }

                if (context.Request.Query.TryGetValue("Tracking-Id", out var trackingId))
                {
                    context.Request.Headers.Add("Tracking-Id", trackingId.First());
                }

                // this will fetch the "headerworkorderid" from the query string and push that into http headers.
                if (context.Request.Query.TryGetValue("headerworkorderid", out var headerworkorderid))
                {
                    // only add into the header if its not in the header.
                    if (!context.Request.Headers.TryGetValue("workorderid", out var workorderid))
                    {
                        context.Request.Headers.Add("workorderid", headerworkorderid.First());
                    }
                }

                // look for CSRF header in the query for few use cases.
                if (context.Request.Query.TryGetValue("headercsrftoken", out var csrfToken))
                {
                    context.Request.Headers.Add(CsrfConstants.CSRFHEADERTOKEN, csrfToken.First());
                }
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
