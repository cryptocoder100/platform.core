namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    // https://github.com/aspnet/Security/blob/rel/2.0.0/src/Microsoft.AspNetCore.Authentication/AuthenticationMiddleware.cs

    /// <summary>
    /// Middleware to Handle Multiple types of authentication.
    /// </summary>
    public class MultiAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationSchemeProvider _schemes;
        private readonly MultiAuthenticationOptions _multiAuthenticationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiAuthenticationMiddleware"/> class.
        /// </summary>
        /// <param name="next">RequestDelegate.</param>
        /// <param name="schemes">IAuthenticationSchemeProvider.</param>
        /// <param name="options">MultiAuthenticationOptions.</param>
        public MultiAuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes, IOptions<MultiAuthenticationOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _multiAuthenticationOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// Catch any failure and return a formatted error response in JSON format.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <param name="context">HttpContext.</param>
        public async Task Invoke(HttpContext context)
        {
            if (context != null)
            {
                context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                {
                    OriginalPath = context.Request.Path,
                    OriginalPathBase = context.Request.PathBase,
                });

                var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                foreach (var scheme in await _schemes.GetRequestHandlerSchemesAsync().ConfigureAwait(false))
                {
                    if (await handlers.GetHandlerAsync(context, scheme.Name).ConfigureAwait(false) is IAuthenticationRequestHandler handler
                        && await handler.HandleRequestAsync().ConfigureAwait(false))
                    {
                        return;
                    }
                }

                // Authenticate against each scheme requested and stop when the first is successful
                foreach (var scheme in _multiAuthenticationOptions.Schemes)
                {
                    var result = await context.AuthenticateAsync(scheme).ConfigureAwait(false);
                    if (result?.Principal != null)
                    {
                        context.User = result.Principal;
                        break;
                    }
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}
