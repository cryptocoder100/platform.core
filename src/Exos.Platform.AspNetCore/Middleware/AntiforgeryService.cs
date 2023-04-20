namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This service contains the logic for csrf validation.
    /// </summary>
    public class AntiforgeryService : IAntiforgeryService
    {
        private readonly ILogger _logger;
        private readonly ExosAntiforgeryOptions _options;
        private readonly IEncryption _encryption;
        private readonly List<AntiforgeryRuleModel> _ignoreRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="AntiforgeryService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="options">ExosAntiforgeryOptions.</param>
        /// <param name="encryption">IEncryption implementation.</param>
        public AntiforgeryService(ILogger<AntiforgeryService> logger, IOptions<ExosAntiforgeryOptions> options, IEncryption encryption)
        {
            _logger = logger;
            _options = options?.Value;
            _encryption = encryption;
            _ignoreRules = new List<AntiforgeryRuleModel>();

            if (_options != null)
            {
                // Compile the regular expressions
                var proxyOptions = _options;
                if (proxyOptions != null && proxyOptions.IgnoreRoutes != null)
                {
                    foreach (var route in proxyOptions.IgnoreRoutes)
                    {
                        if (string.IsNullOrEmpty(route.InputPattern))
                        {
                            continue;
                        }

                        route.Methods = route.Methods ?? new List<string> { "OPTIONS", "HEAD", "GET", "PUT", "POST", "DELETE", "PATCH" };
                        _ignoreRules.Add(new AntiforgeryRuleModel
                        {
                            Pattern = new Regex(route.InputPattern, RegexOptions.IgnoreCase),
                            IgnoreRoute = route,
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Generates the token based on the input.
        /// </summary>
        /// <param name="userInfo">User Info.</param>
        /// <returns>Encoded and Encrypted user info.</returns>
        public string GenerateCsrfToken(string userInfo)
        {
            return HttpUtility.HtmlEncode(_encryption.Encrypt(userInfo));
        }

        /// <summary>
        /// Validates/enforces the csrf headers.
        /// </summary>
        /// <param name="httpContext">HttpContext.</param>
        public void ValidateRequestAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var pathAndQuery = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
            if (_options != null)
            {
                var identity = httpContext.User?.Identity;

                _logger.LogTrace("ValidateAntiforgeryTokenMiddleware running for '{PathAndQuery}'.", pathAndQuery);

                // not enabled, just return
                if (!_options.IsEnabled)
                {
                    _logger.LogDebug("ValidateAntiforgeryTokenMiddleware will not process '{PathAndQuery}' because it is not enabled.", pathAndQuery);
                    return;
                }

                // anonymous route, please ignore.
                if (identity == null || !identity.IsAuthenticated)
                {
                    _logger.LogDebug("ValidateAntiforgeryTokenMiddleware cannot process '{PathAndQuery}' because the request is not authenticated.", pathAndQuery);
                    return;
                }

                if (!IsRelevantAuthScheme(httpContext))
                {
                    _logger.LogWarning("ValidateAntiforgeryTokenMiddleware cannot process '{PathAndQuery}' because the cookie does not have the necessary auth scheme.", pathAndQuery);
                    return;
                }

                // ignore the path, just return.
                var ignore = GetIgnoreRule(httpContext.Request);
                if (ignore != null)
                {
                    _logger.LogDebug("ValidateAntiforgeryTokenMiddleware will not process '{PathAndQuery}' because the path is configured to be ignored.", pathAndQuery);
                    return;
                }

                var csrfHeader = httpContext.Request.Headers[CsrfConstants.CSRFHEADERTOKEN];

                if (string.IsNullOrEmpty(csrfHeader))
                {
                    // no csrf header
                    throw new AntiforgeryValidationException($"Request does not contain CSRF tokens for '{pathAndQuery}'.");
                }
                else
                {
                    // csrf header found, lets validate
                    _logger.LogDebug("ValidateAntiforgeryTokenMiddleware is validating for '{PathAndQuery}'.", pathAndQuery);
                    _encryption.Decrypt(HttpUtility.HtmlDecode(csrfHeader));
                }
            }
            else
            {
                _logger.LogDebug("ValidateAntiforgeryTokenMiddleware will not process '{PathAndQuery}' because the options are null.", pathAndQuery);
                return;
            }
        }

        /// <summary>
        /// Checks the relevant auth scheme.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>True if originalauthschemename claim is the HttpContext.</returns>
        private static bool IsRelevantAuthScheme(HttpContext context)
        {
            bool isRelevant = false;
            var identity = context.User?.Identity;

            // Only validate this exos cookie scheme.
            if (identity != null && context.User != null && context.User.Claims != null
                && context.User.Claims.Any()
                && context.User.Claims.Where(c => c.Type == ClaimConstants.OriginalAuthSchemeName
                && c.Value == PlatformAuthScheme.ExosCookies.ToString()).FirstOrDefault() != null)
            {
                isRelevant = true;
            }

            return isRelevant;
        }

        /// <summary>
        /// Return a Rule to ignore.
        /// </summary>
        /// <param name="request">HttpRequest.</param>
        /// <returns>AntiforgeryRuleModel if there is a rule to ignore in the request.</returns>
        private AntiforgeryRuleModel GetIgnoreRule(HttpRequest request)
        {
            // Find a matching URL in the routes table
            var pathAndQuery = $"{request.Path}{request.QueryString}";
            _logger.LogTrace("Looking for a route to match request for '{PathAndQuery}'...", pathAndQuery);
            var rule = _ignoreRules.FirstOrDefault(r => r.Pattern.IsMatch(pathAndQuery) && r.IgnoreRoute.Methods.Contains(request.Method, StringComparer.OrdinalIgnoreCase));
            return rule;
        }
    }
}
