#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Exos.Platform.AspNetCore.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    [Obsolete("This JWT implementation does not perform encryption. Do not use it for sensitive information.")]
    public class JwtEncryption : IEncryption
    {
        private const string _tokenName = "Token";
        private readonly TokenOptions _tokenOptions;
        private readonly ILogger<JwtEncryption> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtEncryption"/> class.
        /// </summary>
        /// <param name="tokenOptions">Token Options from configuration.</param>
        /// <param name="logger">Logger instance.</param>
        public JwtEncryption(IOptions<TokenOptions> tokenOptions, ILogger<JwtEncryption> logger)
        {
            if (tokenOptions == null)
            {
                throw new ArgumentNullException(nameof(tokenOptions));
            }

            _tokenOptions = tokenOptions.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public string Decrypt(string encryptedString)
        {
            try
            {
                JwtTokenHandler tokenHandler = new JwtTokenHandler(_tokenOptions);
                var claims = tokenHandler.ValidateToken(encryptedString).ToList();
                return claims.FirstOrDefault(c => c.Type == _tokenName.ToLowerInvariant())?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "token failed validation. Message: {Message}", ex?.Message);
                throw new UnauthorizedException("Invalid token.", ex);
            }
        }

        /// <inheritdoc/>
        public string Encrypt(string stringToEncrypt)
        {
            var now = DateTimeOffset.Now;
            var expires = now.AddMinutes(_tokenOptions.JwtLifetimeInMinutes);
            var tokenHandler = new JwtTokenHandler(_tokenOptions);
            List<Claim> clm = new List<Claim>
            {
                new Claim(_tokenName, stringToEncrypt),
            };
            return tokenHandler.CreateToken(expires, clm);
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase