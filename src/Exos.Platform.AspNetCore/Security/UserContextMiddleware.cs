#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable CA1506 // Avoid excessive class coupling
#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1502 // Avoid excessive complexity
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable CA1502 // Avoid excessive complexity

namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Azure.Security.KeyVault.Keys.Cryptography;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.KeyVault;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Add User Context to Request Pipeline.
    /// </summary>
    public class UserContextMiddleware
    {
        private static readonly Regex _authorizationHeaderRegex = new Regex(@"^(\w+)\s+(.*)", RegexOptions.IgnoreCase);
        private readonly RequestDelegate _next;
        private readonly ILogger<UserContextMiddleware> _logger;
        private readonly UserContextOptions _options;
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly AzureKeyVaultKeyClient _keyVaultKeyClient;
        private readonly HttpClient _httpClient;
        private readonly HttpClient _httpClientGetClaims;
        private readonly Regex _ignoreInputPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextMiddleware"/> class.
        /// </summary>
        /// <param name="next"><see cref="RequestDelegate"/>.</param>
        /// <param name="logger"><see cref="ILogger{UserContextMiddleware}"/>.</param>
        /// <param name="optionsAccessor"><see cref="IOptions{TOptions}"/>.</param>
        /// <param name="distributedCache"><see cref="IDistributedCache"/>.</param>
        /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/>.</param>
        /// <param name="keyVaultKeyClient"><see cref="AzureKeyVaultKeyClient"/>.</param>
        /// <param name="memoryCache">memoryCache.</param>
        public UserContextMiddleware(
            RequestDelegate next,
            ILogger<UserContextMiddleware> logger,
            IOptions<UserContextOptions> optionsAccessor,
            IDistributedCache distributedCache,
            IHttpClientFactory httpClientFactory,
            AzureKeyVaultKeyClient keyVaultKeyClient,
            IMemoryCache memoryCache)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));

            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            var localFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _keyVaultKeyClient = keyVaultKeyClient;

            _httpClient = localFactory.CreateClient("ExosNative");
            _httpClientGetClaims = localFactory.CreateClient("AuthOnlyForGetClaims");

            if (!string.IsNullOrEmpty(_options.IgnoreInputPattern))
            {
                _ignoreInputPattern = new Regex(_options.IgnoreInputPattern, RegexOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// Return a Sha256 Hash.
        /// </summary>
        /// <param name="value">The value<see cref="string"/>.</param>
        /// <returns>The byte array containing the Hashing.</returns>
        public static byte[] Sha256Hash(string value)
        {
            using var hash = SHA256.Create();
            var hashedBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            return hashedBytes;
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context != null)
            {
                var request = context.Request;

                // Is this a URL we should process?
                var pathAndQuery = $"{request.Path}{request.QueryString}";
                if (_ignoreInputPattern != null && _ignoreInputPattern.IsMatch(pathAndQuery))
                {
                    // Don't process this request
                    var loggingPayload = LoggerHelper.SanitizeValue(pathAndQuery);
                    _logger.LogTrace($"User Context was not processed for {loggingPayload} because it matched the ignore input pattern.");

                    if (_next != null)
                    {
                        await _next(context).ConfigureAwait(false);
                    }

                    return;
                }

                // Is there a user already provided?
                var explicitUser = context.RequestServices?.GetService<IUserContextProvider>()?.User;
                if (explicitUser != null)
                {
                    context.User = explicitUser;
                    _logger.LogTrace($"User Context was explicitly provided.");

                    if (_next != null)
                    {
                        await _next(context).ConfigureAwait(false);
                    }

                    return;
                }

                await BuildUserContext(context).ConfigureAwait(false);
            }
        }

        private static string GetUsername(HttpContext context)
        {
            var username = context.User?.Identity?.Name;
            var claims = context.User?.Claims?.ToList() ?? new List<Claim>();

            if (string.IsNullOrEmpty(username))
            {
                // Sometimes the claim mapping is setup incorrectly and the name claim isn't put into
                // the user principal name property. Look for the claim directly.
                username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            }

            if (string.IsNullOrEmpty(username))
            {
                // We have some legacy code that incorrectly puts the username in the sub claim
                username = claims.FirstOrDefault(c => c.Type == JwtTokenClaims.Sub)?.Value;
            }

            if (string.IsNullOrEmpty(username))
            {
                // Exos app has user name in Aud field
                username = claims.FirstOrDefault(c => c.Type == JwtTokenClaims.Aud)?.Value;
            }

            return username;
        }

        private static void BuildWorkOrderRelatedClaimsForNonTenantUsers(HttpContext context, List<Claim> targetClaims)
        {
            var claims = context.User?.Claims?.ToList();
            if (claims != null)
            {
                var claimWorkorderIdList = claims.FindAll(c => c.Type == "claimworkorderid");
                if (claimWorkorderIdList != null && claimWorkorderIdList.Count > 0)
                {
                    targetClaims.AddRange(claimWorkorderIdList);
                }
            }
        }

        private static string GetClaim(List<Claim> claims, string claimToLookFor)
        {
            return claims.FirstOrDefault(c => c.Type == claimToLookFor)?.Value;
        }

        private static void ValidateClaim(List<Claim> claims, long targetServicerTenantId)
        {
            if (claims != null && claims.Count > 0)
            {
                var allowedServicers = claims.Where(i => i.Type == "associatedservicertenantidentifier").Select(o => long.Parse(o.Value, CultureInfo.InvariantCulture)).ToList();
                if (targetServicerTenantId > 0 && !allowedServicers.Contains(targetServicerTenantId))
                {
                    throw new UnauthorizedException("Invalid claims, ServicerTenantId = " + targetServicerTenantId.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static PlatformAuthScheme? GetAuthScheme(HttpContext context)
        {
            var claims = context.User?.Claims?.ToList() ?? new List<Claim>();
            var authSchemeClaim = claims.Where(c => c.Type == ClaimConstants.OriginalAuthSchemeName).FirstOrDefault();
            if (authSchemeClaim != null && Enum.TryParse(authSchemeClaim.Value, out PlatformAuthScheme authScheme))
            {
                return authScheme;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader != null)
            {
                var authParts = authHeader.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (authParts.Length == 2)
                {
                    if (Enum.TryParse(authParts[0], out PlatformAuthScheme authSchemeFromHeader))
                    {
                        return authSchemeFromHeader;
                    }
                }
            }

            return null;
        }

        private static void PopulateAuthSchemeClaim(List<Claim> claims, PlatformAuthScheme? authScheme)
        {
            if (authScheme != null)
            {
                claims.Add(new Claim(ClaimConstants.OriginalAuthSchemeName, authScheme.Value.ToString()));
            }
        }

        private static void PopulateWorkOrderClaims(WorkOrderTenancyModel workOrderTenancyModel, List<Claim> claims)
        {
            if (workOrderTenancyModel != null && claims != null)
            {
                if (workOrderTenancyModel.ServicerGroupTenantId != null && workOrderTenancyModel.ServicerGroupTenantId.HasValue)
                {
                    claims.Add(new Claim("woservicergrouptenantid", workOrderTenancyModel.ServicerGroupTenantId.ToString()));
                }

                if (workOrderTenancyModel.SubClientTenantId != null && workOrderTenancyModel.SubClientTenantId.HasValue)
                {
                    claims.Add(new Claim("wosubclienttenantid", workOrderTenancyModel.SubClientTenantId.ToString()));
                }

                if (workOrderTenancyModel.ClientTenantId != null && workOrderTenancyModel.ClientTenantId.HasValue)
                {
                    claims.Add(new Claim("woclienttenantid", workOrderTenancyModel.ClientTenantId.ToString()));
                }

                if (workOrderTenancyModel.VendorTenantId != null && workOrderTenancyModel.VendorTenantId.HasValue)
                {
                    claims.Add(new Claim("wovendortenantid", workOrderTenancyModel.VendorTenantId.ToString()));
                }

                if (workOrderTenancyModel.SubContractorTenantId != null && workOrderTenancyModel.SubContractorTenantId.HasValue)
                {
                    claims.Add(new Claim("wosubcontractortenantid", workOrderTenancyModel.SubContractorTenantId.ToString()));
                }

                if (!string.IsNullOrEmpty(workOrderTenancyModel.SourceSystemWorkOrderNumber))
                {
                    claims.Add(new Claim("sourcesystemworkordernumber", workOrderTenancyModel.SourceSystemWorkOrderNumber));
                }

                if (!string.IsNullOrEmpty(workOrderTenancyModel.SourceSystemOrderNumber))
                {
                    claims.Add(new Claim("sourcesystemordernumber", workOrderTenancyModel.SourceSystemOrderNumber));
                }

                if (workOrderTenancyModel.WorkOrderId != null && workOrderTenancyModel.WorkOrderId.HasValue)
                {
                    claims.Add(new Claim("headerworkorderid", workOrderTenancyModel.WorkOrderId.ToString()));
                }
            }
        }

        private static long CheckForTenantChange(List<Claim> claims, long? servicertenantidentifier, long servicerContextTenantId)
        {
            var tenantType = GetClaim(claims, "tenanttype");
            if (!string.IsNullOrEmpty(tenantType))
            {
                tenantType = tenantType.ToLowerInvariant();
                switch (tenantType)
                {
                    case "exos":
                    case "servicer":
                        {
                            Claim tenantIdClaim = claims.FirstOrDefault(c => c.Type == "tenantidentifier");

                            // override user context
                            if (servicertenantidentifier != null
                                && servicertenantidentifier.HasValue
                                && servicertenantidentifier.Value > 0
                                && tenantIdClaim != null
                                && tenantIdClaim.Value != servicertenantidentifier.ToString())
                            {
                                claims.Remove(tenantIdClaim); // remove tenant claim
                                claims.Add(new Claim("tenantidentifier", servicertenantidentifier.ToString())); // add tenant claims
                            }

                            var claim = claims.FirstOrDefault(i => i.Type == "tenantidentifier");
                            if (claim != null)
                            {
                                if (long.TryParse(claim.Value, out servicerContextTenantId))
                                {
                                    ValidateClaim(claims, servicerContextTenantId);
                                }
                            }

                            break;
                        }

                    case "vendor":
                    case "subcontractor":
                    case "masterclient":
                    case "subclient":
                        {
                            Claim servicertenantidentifierClaim =
                                claims.FirstOrDefault(c => c.Type == "servicertenantidentifier");
                            if (servicertenantidentifier != null
                                && servicertenantidentifier.HasValue
                                && servicertenantidentifier.Value > 0
                                && servicertenantidentifierClaim != null
                                && servicertenantidentifierClaim.Value != servicertenantidentifier.ToString())
                            {
                                // override user context
                                claims.Remove(servicertenantidentifierClaim); // remove tenant claim
                                claims.Add(new Claim("servicertenantidentifier", servicertenantidentifier.ToString())); // add tenant claims
                            }

                            var servicerTenantIdentifierWhenSub =
                                claims.FirstOrDefault(i => i.Type == "servicertenantidentifier");
                            if (servicerTenantIdentifierWhenSub != null)
                            {
                                if (long.TryParse(servicerTenantIdentifierWhenSub.Value, out servicerContextTenantId))
                                {
                                    ValidateClaim(claims, servicerContextTenantId);
                                }
                            }

                            break;
                        }
                }
            }

            return servicerContextTenantId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long? ParseNullableInt64(string val)
        {
            if (val == null)
            {
                return null;
            }

            // Not using TryParse because want invalid data to throw an exception
            return long.Parse(val, CultureInfo.InvariantCulture);
        }

        private static long GetDefaultTenantId(UserModel userModel)
        {
            long tenantid = 0;

            // simple servicer/vendor/sub/client user.
            if (userModel.TenantId > 0)
            {
                tenantid = userModel.TenantId;
            }
            else
            {
                // EXOS ADMIN.
                if (userModel.TenantIds != null && userModel.TenantIds.Count > 0)
                {
                    tenantid = userModel.TenantIds[0];
                }
            }

            return tenantid;
        }

        private static void PopulateServicerContextClaims(dynamic entityProfile, List<Claim> claims, long? servicerContextTenantId)
        {
            if (entityProfile != null && entityProfile.servicerAssociations != null && claims != null)
            {
                List<dynamic> servicerAssociations = new List<dynamic>();
                foreach (var servicerAssociation in entityProfile.servicerAssociations)
                {
                    servicerAssociations.Add(servicerAssociation);
                }

                servicerAssociations = servicerAssociations
                    .Where(i => (i.audit != null && i.audit.isDeleted == false) || (i.audit == null)).ToList();
                if (servicerAssociations != null && servicerAssociations.Count > 0)
                {
                    foreach (var servicerTenantObj in servicerAssociations)
                    {
                        // add all servicers in the claims.
                        if (servicerTenantObj != null)
                        {
                            if (servicerTenantObj.servicerId > 0)
                            {
                                claims.Add(new Claim("associatedservicertenantidentifier", servicerTenantObj.servicerId.ToString()));
                            }
                        }
                    }

                    // default to first one.
                    if ((servicerContextTenantId == null || servicerContextTenantId <= 0) &&
                        servicerAssociations.Count > 0)
                    {
                        // Default to vendor's first servicer.
                        var servicerTenantObj = servicerAssociations.ToList()[0];
                        if (servicerTenantObj.servicerId > 0)
                        {
                            claims.Add(new Claim("servicertenantidentifier", servicerTenantObj.servicerId.ToString()));
                        }
                    }
                    else
                    {
                        // specified in the claims.
                        if (servicerContextTenantId != null && servicerContextTenantId > 0)
                        {
                            claims.Add(new Claim("servicertenantidentifier", servicerContextTenantId.ToString()));
                        }
                    }
                }
            }
        }

        private static HttpRequestMessage GetAuthRequestMessage(HttpContext context, HttpMethod method, string requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            string accessToken = string.Empty;
            string accessTokenViaApiKey = string.Empty;
            var authScheme = GetAuthScheme(context);
            if (authScheme == PlatformAuthScheme.ApiKey)
            {
                accessTokenViaApiKey = context.User.Claims
                    .FirstOrDefault(c => c.Type == ClaimConstants.ApiKeyAccessToken)?.Value;
            }
            else if (authScheme == PlatformAuthScheme.Bearer)
            {
                accessToken = _authorizationHeaderRegex.Match(context.Request.Headers["Authorization"])
                    .Groups[2].Value;
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authScheme.ToString(), accessToken);
            }

            return request;
        }

        private async Task BuildUserContext(HttpContext context)
        {
            var loggerAttributes = new Dictionary<string, object>();

            // In this configuration, a user context is constructed from the username of the authenticated user.
            // Authentication happens prior to this middleware and we must call several user-related services to build context.
            var authScheme = GetAuthScheme(context);
            var username = GetUsername(context);

            WorkOrderTenancyModel workOrderTenancyModel = null;

            var workorderidentifier = context.GetWorkOrderId(); // check if any workorderid is passed.

            if (workorderidentifier != null && workorderidentifier.HasValue && workorderidentifier > 0)
            {
                loggerAttributes["WorkOrderId"] = LoggerHelper.SanitizeValue(workorderidentifier);
                TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("WorkOrderId", ((long)workorderidentifier).ToString(CultureInfo.InvariantCulture)));

                if (TryPopulateWorkOrderTenantFromHeader(context, (long)workorderidentifier, out workOrderTenancyModel))
                {
                    loggerAttributes["WorkOrderTenantSource"] = "Header";
                    TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("WorkOrderTenantSource", "Header"));
                }
                else
                {
                    var orderUrl = _options.OrderManagementSvc + "/api/v1/commonordersvc/workorder/tenant/" + workorderidentifier;
                    using (var request = new HttpRequestMessage(HttpMethod.Get, orderUrl))
                    {
                        request.Headers.TryAddWithoutValidation("Tracking-Id", context.GetTrackingId());
                        _logger.LogTrace("Calling '{ValuationOrdersvc}' for user record.", LoggerHelper.SanitizeValue(orderUrl));
                        using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            workOrderTenancyModel = await response.Content.ReadAsJsonAsync<WorkOrderTenancyModel>().ConfigureAwait(false);
                            _logger.LogTrace("Got tenancy info for '{WorkOrderId}'.", LoggerHelper.SanitizeValue(workorderidentifier));
                        }
                    }

                    loggerAttributes["WorkOrderTenantSource"] = "Service";
                    TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("WorkOrderTenantSource", "Service"));
                }
            }

            long servicerContextTenantId = 0;
            var servicertenantidentifier = context.GetServicerContextTenantId(); // check if any servicer is in passed.

            // workorder tenancy takes over.
            if (workOrderTenancyModel != null)
            {
                servicertenantidentifier = workOrderTenancyModel.ServicerTenantId;
            }

            if (servicertenantidentifier != null && servicertenantidentifier > 0)
            {
                loggerAttributes["ServicerTenantId"] = LoggerHelper.SanitizeValue(servicertenantidentifier);
                TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("ServicerTenantId", ((long)servicertenantidentifier).ToString(CultureInfo.InvariantCulture)));
            }

            if (string.IsNullOrEmpty(username))
            {
                // This is not an authenticated request
                _logger.LogTrace("User Context was not constructed because the request is not authenticated.");

                if (_next != null)
                {
                    using var loggerScope = _logger.BeginScope(loggerAttributes);
                    await _next(context).ConfigureAwait(false);
                }

                return;
            }

            List<Claim> claims = null;
            string uri = null;
            string accessToken = null;
            try
            {
                var accessTokenViaApiKey = string.Empty;
                try
                {
                    if (authScheme == PlatformAuthScheme.ApiKey)
                    {
                        accessTokenViaApiKey = context.User.Claims
                            .FirstOrDefault(c => c.Type == ClaimConstants.ApiKeyAccessToken)?.Value;
                    }

                    if (authScheme == PlatformAuthScheme.Bearer || (authScheme == PlatformAuthScheme.ApiKey &&
                                                            string.IsNullOrEmpty(accessTokenViaApiKey) == false))
                    {
                        if (authScheme == PlatformAuthScheme.Bearer)
                        {
                            accessToken = _authorizationHeaderRegex.Match(context.Request.Headers["Authorization"])
                                .Groups[2].Value;
                        }
                        else
                        {
                            accessToken = accessTokenViaApiKey;
                        }

                        // check if the token has been revoked
                        var isRevoked = await TokenRevoker.IsTokenRevoked(accessToken, _distributedCache).ConfigureAwait(false);
                        if (isRevoked)
                        {
                            throw new UnauthorizedException("Session already revoked.");
                        }

                        // Look for user claims in cache
                        var cacheKey = $"UserClaimsCacheKey:'{accessToken}'";
                        var claimsBuffer = await _distributedCache.GetAsync(cacheKey);

                        if (claimsBuffer != null && claimsBuffer.Length > 0)
                        {
                            var cachedClaims = new List<Claim>(250);
                            using var sha256 = SHA256.Create();
                            var claimsReader = new UserClaimsReader(claimsBuffer, cachedClaims, sha256);
                            claimsReader.Read();

                            var signatureVerified = VerifySignature(claimsReader.Hash, claimsReader.Signature);
                            if (signatureVerified)
                            {
                                claims = new List<Claim>(cachedClaims);
                                claims.Add(new Claim(ExosClaimTypes.ClaimsSignature, claimsReader.SignatureValue));
                            }
                            else
                            {
                                _logger.LogError("Cached claims not verified for " +
                                                 $"{LoggerHelper.SanitizeValue(username)} ");
                            }
                        }
                    }
                }
                catch (UnauthorizedException ex)
                {
                    string exceptionMessage = ex?.Message;
                    _logger.LogError(
                        ex,
                        "User has logged out. User: {Username}, Uri: {Uri}, Message: {Message}.",
                        LoggerHelper.SanitizeValue(username),
                        LoggerHelper.SanitizeValue(uri),
                        LoggerHelper.SanitizeValue(exceptionMessage));
                    throw;
                }
                catch (Exception ex)
                {
                    string exceptionMessage = ex?.Message;
                    _logger.LogError(
                        ex,
                        "Failure getting cached claims. User: {Username}, Uri: {Uri}, Message: {Message}.",
                        LoggerHelper.SanitizeValue(username),
                        LoggerHelper.SanitizeValue(uri),
                        LoggerHelper.SanitizeValue(exceptionMessage));
                }

                // If no claims in cache
                if (claims == null || claims.Count == 0)
                {
                    claims = await GetInitialClaimsFromUserSvc(context, username, servicertenantidentifier);
                    servicerContextTenantId = (servicertenantidentifier != null && servicertenantidentifier.HasValue) ? servicertenantidentifier.Value : 0;
                }
                else
                {
                    // If servicer tenant id has been flipped, refresh existing claims.
                    servicerContextTenantId =
                        CheckForTenantChange(claims, servicertenantidentifier, servicerContextTenantId);
                }

                if (string.IsNullOrEmpty(accessTokenViaApiKey) == false)
                {
                    claims.Add(new Claim(ClaimConstants.ApiKeyAccessToken, accessTokenViaApiKey));
                }

                if (servicerContextTenantId > 0)
                {
                    await PopulateServicerProfileClaims(context, servicerContextTenantId, claims)
                        .ConfigureAwait(false); // Add servicer features
                }
            }
            catch (Exception ex)
            {
                string exceptionMessage = ex?.Message;
                _logger.LogWarning(
                    ex,
                    "User context generation failed with an error. User: {Username}, Uri: {Uri}, Message: {Message}.",
                    LoggerHelper.SanitizeValue(username),
                    LoggerHelper.SanitizeValue(uri),
                    LoggerHelper.SanitizeValue(exceptionMessage));
                throw;
            }

            // always fresh, not caching it unless we first invalidate them.
            PopulateWorkOrderClaims(workOrderTenancyModel, claims);

            if (!string.IsNullOrEmpty(accessToken))
            {
                // always fresh claims added to the user context for non-proper tenants like for Consumer app.
                await PopulateWorkOrderIdsFromRedis(claims, accessToken);
            }

            // populate auth scheme
            PopulateAuthSchemeClaim(claims, authScheme);
            // Set the current user
            var claimsIdentity = new ClaimsIdentity(claims, PlatformAuthScheme.Jwt.ToString());
            context.User = new ClaimsPrincipal(claimsIdentity);
            _logger.LogDebug("User Context has been constructed for '{Username}' and added to the request.", LoggerHelper.SanitizeValue(context?.User?.Identity?.Name));

            // Execute the remainder of the request
            if (_next != null)
            {
                using var loggerScope = _logger.BeginScope(loggerAttributes);
                await _next(context).ConfigureAwait(false);
            }
        }

        private bool TryPopulateWorkOrderTenantFromHeader(HttpContext context, long workOrderIdentifier, out WorkOrderTenancyModel workOrderTenancyModel)
        {
            workOrderTenancyModel = null;

            // Do we meet preconditions for receiving tenant information in a header?
            if (!_options.AcceptInboundWorkOrderTenantHeader)
            {
                _logger.LogDebug("The 'Exos-Work-Order-Tenant' inbound header will not be accepted because the feature is disabled.");
                return false;
            }

            if (!context.Request.Headers.TryGetValue("Exos-Work-Order-Tenant", out var workOrderTenantHeader) || workOrderTenantHeader.Count < 1)
            {
                _logger.LogDebug("The 'Exos-Work-Order-Tenant' inbound header is not present in the request.");
                return false;
            }

            var key = UserContextHelper.GetWorkOrderTenantHeaderSigningSecretBytesSafe(_options); // Null-safe
            if (key == null || key.Length == 0)
            {
                _logger.LogWarning("The 'Exos-Work-Order-Tenant' inbound header cannot be accepted because a signing secret is not configured.");
                return false;
            }

            try
            {
                var entity = UserContextHelper.VerifyAndParseWorkOrderTenant(workOrderTenantHeader.First(), key); // Throws on failure
                workOrderTenancyModel = new WorkOrderTenancyModel
                {
                    ServicerGroupTenantId = ParseNullableInt64(entity.ServicerGroupTenantId),
                    VendorTenantId = ParseNullableInt64(entity.VendorTenantId),
                    SubContractorTenantId = ParseNullableInt64(entity.SubContractorTenantId),
                    ClientTenantId = ParseNullableInt64(entity.ClientTenantId),
                    SubClientTenantId = ParseNullableInt64(entity.SubClientTenantId),
                    WorkOrderId = ParseNullableInt64(entity.WorkOrderId),
                    SourceSystemWorkOrderNumber = entity.SourceSystemWorkOrderNumber,
                    SourceSystemOrderNumber = entity.SourceSystemOrderNumber
                };

                // This should never happen given our outbound logic, but we check for it anyway
                if (workOrderIdentifier != workOrderTenancyModel.WorkOrderId)
                {
                    throw new InvalidOperationException($"The 'Exos-Work-Order-Tenant' inbound header work order ID does not match the work order ID in the inbound 'workorderid' header.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"There was an error parsing or validating the 'Exos-Work-Order-Tenant' header for the work order ID '{{WorkOrderId}}'. " +
                    $"Given the possibility of a security issue (attack), the request will be aborted.",
                    LoggerHelper.SanitizeValue(workOrderIdentifier));

                throw;
            }
        }

        private async Task PopulateWorkOrderIdsFromRedis(List<Claim> claims, string accessToken)
        {
            var cacheKey = $"UserClaimsWorkOrdersCacheKey:'{accessToken}'";
            var workOrderIdsString = await _distributedCache.GetStringAsync(cacheKey).ConfigureAwait(false);

            if (workOrderIdsString == null)
            {
                return;
            }

            var workOrderIds = JsonSerializer.Deserialize<List<string>>(workOrderIdsString);

            foreach (var workOrderId in workOrderIds)
            {
                claims.Add(new Claim("claimworkorderid", workOrderId.Trim()));
            }
        }

        private bool VerifySignature(byte[] hashedClaims, byte[] signature)
        {
            var cc = _keyVaultKeyClient.GetCryptographyClient(_options.KeyVaultKeyName);

            var verified = cc.Verify(SignatureAlgorithm.RS256, hashedClaims, signature);
            return verified.IsValid;
        }

        private async Task<List<Claim>> GetInitialClaimsFromUserSvc(HttpContext context, string username, long? servicertenantidentifier)
        {
            var query = new Dictionary<string, string> { ["username"] = username };
            if (servicertenantidentifier.HasValue)
            {
                query["servicerTenantId"] = servicertenantidentifier.Value.ToString(CultureInfo.InvariantCulture);
            }

            var uri = _options.UserSvc + QueryHelpers.AddQueryString("/api/v1/contexts/UserClaims/", query);

            using var request = GetAuthRequestMessage(context, HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("Tracking-Id", context.GetTrackingId());
            _logger.LogTrace("Calling '{UserSvc}' for user claims.", LoggerHelper.SanitizeValue(uri));

            using var response = await _httpClientGetClaims.SendAsync(request).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            var userClaims = await response.Content.ReadAsJsonAsync<UserClaims>().ConfigureAwait(false);
            _logger.LogTrace("Got user claims for '{Username}'.", LoggerHelper.SanitizeValue(username));

            var claims = userClaims.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();

            return claims;
        }

        private async Task PopulateServicerProfileClaims(HttpContext context, long? servicerContextTenantId, List<Claim> claims)
        {
            // pull servicer features in the context.
            if (servicerContextTenantId != null && servicerContextTenantId.HasValue && servicerContextTenantId > 0 && claims != null)
            {
                string servicerProfileCacheKey = $"{nameof(UserContextMiddleware)}.UserContextServicerProfileCachKey:'{servicerContextTenantId.ToString().Trim().ToLowerInvariant()}'";

                if (!_memoryCache.TryGetValue(servicerProfileCacheKey, out List<Claim> servicerProfileClaims))
                {
                    servicerProfileClaims = new List<Claim>();
                    string uri = _options.UserSvc + "/api/v1/contexts/Servicer/" + servicerContextTenantId;
                    using (var request = GetAuthRequestMessage(context, HttpMethod.Get, uri))
                    {
                        request.Headers.TryAddWithoutValidation("Tracking-Id", context.GetTrackingId());
                        _logger.LogTrace("Calling '{UserSvc}' for servicer record.", LoggerHelper.SanitizeValue(uri));
                        using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            var servicerProfiles = await response.Content.ReadAsJsonAsync<List<ServicerProfile>>().ConfigureAwait(false);
                            _logger.LogTrace("Got servicer info for '{servicerId}'.", LoggerHelper.SanitizeValue(servicerContextTenantId));

                            if (servicerProfiles != null && servicerProfiles.Count > 0)
                            {
                                var servicerProfile = servicerProfiles[0];
                                if (servicerProfile.SubscribedFeatures != null && servicerProfile.SubscribedFeatures.Count > 0)
                                {
                                    foreach (var feature in servicerProfile.SubscribedFeatures)
                                    {
                                        servicerProfileClaims.Add(new Claim("servicerfeature", feature.Id));
                                        if (feature.Feature != null && feature.Feature.Resources != null && feature.Feature.Resources.Count > 0)
                                        {
                                            // should we add this to role claims?
                                            feature.Feature.Resources.ForEach(rsc =>
                                            {
                                                servicerProfileClaims.Add(new Claim("servicerfeature", rsc.Id));
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Add Servicer claims to memory cache
                    double reloadInterval = _options.CachedServicerClaimsLifetimeInMinutes;
                    var cacheExpirationOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddMinutes(reloadInterval),
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(reloadInterval - 2)
                    };
                    _memoryCache.Set(servicerProfileCacheKey, servicerProfileClaims, cacheExpirationOptions);
                }

                if (claims != null && servicerProfileClaims != null && servicerProfileClaims.Count > 0)
                {
                    claims.AddRange(servicerProfileClaims);
                }
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning restore SA1118 // Parameter should not span multiple lines
#pragma warning restore CA1506 // Avoid excessive class coupling
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning disable CA1502 // Avoid excessive complexity