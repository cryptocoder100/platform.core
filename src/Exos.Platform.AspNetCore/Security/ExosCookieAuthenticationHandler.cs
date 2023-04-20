#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetCore.LegacyAuthCookieCompat;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FormsAuthenticationTicket = AspNetCore.LegacyAuthCookieCompat.FormsAuthenticationTicket;

namespace Exos.Platform.AspNetCore.Security
{
    // $/FieldServices/SpaLogin/Dev4_EXOS_RC1/src/RestWebAPIs/Login.RestWebAPIs/Controllers/CustomAuthController.cs
    // $/FieldServices/CommonLib/Dev4_EXOS_RC1/src/CommonLib/WebUtilities/Helper.cs
    // https://github.com/aspnet/Security/blob/rel/2.0.0/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationHandler.cs
    // https://github.com/dazinator/AspNetCore.LegacyAuthCookieCompat

    /// <summary>
    /// Exos Cookie Authentication Handler.
    /// </summary>
    public class ExosCookieAuthenticationHandler : AuthenticationHandler<ExosCookieAuthenticationOptions>
    {
        private readonly ILogger<ExosCookieAuthenticationHandler> _logger;
        private Task<AuthenticateResult> _resultTask;
        private DateTimeOffset _refreshIssuedUtc;
        private DateTimeOffset _refreshExpiresUtc;
        private bool _shouldRefresh;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosCookieAuthenticationHandler"/> class.
        /// </summary>
        /// <param name="options">ExosCookieAuthenticationOptions.</param>
        /// <param name="logger">ILoggerFactory.</param>
        /// <param name="encoder">UrlEncoder.</param>
        /// <param name="clock">ISystemClock.</param>
        public ExosCookieAuthenticationHandler(IOptionsMonitor<ExosCookieAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _logger = logger.CreateLogger<ExosCookieAuthenticationHandler>();
        }

        /// <inheritdoc/>
        protected override Task InitializeHandlerAsync()
        {
            Context.Response.OnStarting(FinishResponseAsync);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var result = await EnsureResult().ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Invoked just before response headers will be sent to the client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task FinishResponseAsync()
        {
            if (!_shouldRefresh)
            {
                return;
            }

            var result = await EnsureResult().ConfigureAwait(false);
            var properties = result.Properties;

            var legacyTicket = new FormsAuthenticationTicket(
                int.Parse(properties.Items["Version"], CultureInfo.InvariantCulture),
                properties.Items["Name"],
                _refreshIssuedUtc.LocalDateTime,
                _refreshExpiresUtc.LocalDateTime,
                properties.IsPersistent,
                properties.Items["UserData"],
                properties.Items["CookiePath"]);

            var encryptor = new LegacyFormsAuthenticationTicketEncryptor(Options.DecryptionKey, Options.ValidationKey);
            var cookie = encryptor.Encrypt(legacyTicket);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Path = legacyTicket.CookiePath,
                Secure = Options.Secure,
                Domain = Options.Domain ?? Context.Request.Host.Host,
                SameSite = SameSiteMode.None,
            };

            Context.Response.Cookies.Append(Options.Name, cookie, cookieOptions);

            _logger.LogDebug(
                "EXOS cookie has been refreshed in response. Path: '{Path}', Secure: '{Secure}', Domain: '{Domain}'",
                LoggerHelper.SanitizeValue(cookieOptions?.Path),
                LoggerHelper.SanitizeValue(cookieOptions?.Secure),
                LoggerHelper.SanitizeValue(cookieOptions?.Domain));
        }

        /// <summary>
        /// Contains the result of an Authenticate call reading a cookie.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task<AuthenticateResult> EnsureResult()
        {
            // Read the cookie just once
            if (_resultTask == null)
            {
                _resultTask = ReadCookieTicket();
            }

            return _resultTask;
        }

        /// <summary>
        /// Read a Cookie Ticket.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<AuthenticateResult> ReadCookieTicket()
        {
            if (!Context.Request.Cookies.TryGetValue(Options.Name, out string cookie) || string.IsNullOrEmpty(cookie))
            {
                // No cookie was provided
                _logger.LogDebug("EXOS cookie was not found or was empty.");
                return AuthenticateResult.NoResult();
            }

            FormsAuthenticationTicket legacyTicket;
            UserContext userContext;
            try
            {
                var encryptor = new LegacyFormsAuthenticationTicketEncryptor(Options.DecryptionKey, Options.ValidationKey);
                legacyTicket = encryptor.DecryptCookie(cookie);
                // Bug# 263092 - Performance-: Functional NPE - Security.ExosCookieAuthenticationHandler
                if (legacyTicket == null)
                {
                    return AuthenticateResult.Fail("EXOS cookie authentication failed during decryption. LegacyTicket is null after decrypting the cookie.");
                }

                userContext = JsonSerializer.Deserialize<UserContext>(legacyTicket.UserData);
            }
            catch (Exception ex)
            {
                // Any exception here would just be garbage (e.g. System.Exception, CryptographicException, etc.)
                _logger.LogWarning(ex, "EXOS cookie authentication failed during decryption. Message: {Message}", ex.Message);
                return AuthenticateResult.Fail("Cookie was not in a valid format.");
            }

            var currentUtc = Clock.UtcNow;
            var issuedUtc = new DateTimeOffset(legacyTicket.IssueDate);
            var expiresUtc = new DateTimeOffset(legacyTicket.Expiration);

            if (expiresUtc < currentUtc)
            {
                _logger.LogWarning("EXOS cookie authentication failed because the cookie has expired.");
                return AuthenticateResult.Fail("Cookie has expired.");
            }

            // Place into a modern ticket
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userContext.UserName),
                new Claim(ClaimConstants.OriginalAuthSchemeName, Scheme.Name),
            };
            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            _logger.LogDebug("User '{Username}' has been authenticated.", claimsIdentity?.Name);

            var properties = new AuthenticationProperties
            {
                IssuedUtc = issuedUtc,
                ExpiresUtc = expiresUtc,
                IsPersistent = legacyTicket.IsPersistent,
                AllowRefresh = true,
            };
            properties.Items.Add("Version", legacyTicket.Version.ToString(CultureInfo.InvariantCulture));
            properties.Items.Add("UserData", legacyTicket.UserData);
            properties.Items.Add("Name", legacyTicket.Name);
            properties.Items.Add("CookiePath", legacyTicket.CookiePath);

            var ticket = new AuthenticationTicket(claimsPrincipal, properties, Scheme.Name);

            CheckForRefresh(ticket);

            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// Check if cookie needs to be refreshed.
        /// </summary>
        /// <param name="ticket">AuthenticationTicket.</param>
        private void CheckForRefresh(AuthenticationTicket ticket)
        {
            var currentUtc = Clock.UtcNow;
            var issuedUtc = ticket.Properties.IssuedUtc;
            var expiresUtc = ticket.Properties.ExpiresUtc;
            var allowRefresh = ticket.Properties.AllowRefresh ?? true;
            if (issuedUtc != null && expiresUtc != null && allowRefresh)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                var timeRemaining = expiresUtc.Value.Subtract(currentUtc);

                if (timeRemaining < timeElapsed)
                {
                    _logger.LogDebug("EXOS cookie will be refreshed in response. Time Remaining: '{TimeRemaining}', Time Elapsed: '{TimeElapsed}'", timeRemaining, timeElapsed);

                    // Update the cookie in the response
                    _shouldRefresh = true;
                    _refreshIssuedUtc = currentUtc;
                    var timeSpan = expiresUtc.Value.Subtract(issuedUtc.Value);
                    _refreshExpiresUtc = currentUtc.Add(timeSpan);
                }
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously