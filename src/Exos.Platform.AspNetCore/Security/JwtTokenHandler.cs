namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Contains functions for creating and validating JSON Web Tokens.
    /// </summary>
    public class JwtTokenHandler
    {
        /// <summary>
        /// Defines the _signingKey.
        /// </summary>
        private readonly SymmetricSecurityKey _signingKey;

        /// <summary>
        /// Defines the _signingCredentials.
        /// </summary>
        private readonly SigningCredentials _signingCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenHandler"/> class.
        /// </summary>
        /// <param name="userContextOptions">The handler options.</param>
        public JwtTokenHandler(UserContextOptions userContextOptions)
        {
            Options = userContextOptions ?? throw new ArgumentNullException(nameof(userContextOptions));
            _signingKey = new SymmetricSecurityKey(ByteConverter.HexStringToBytes(userContextOptions.JwtSigningKey));
            _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        }

        /// <summary>
        /// Gets the <see cref="UserContextOptions" /> being used by this handler.
        /// </summary>
        public UserContextOptions Options { get; }

        /// <summary>
        /// Creates a new JWT.
        /// </summary>
        /// <param name="expires">When the JWT expires.</param>
        /// <param name="claims">The JWT claims (permissions).</param>
        /// <returns>A new JWT.</returns>
        public string CreateToken(DateTimeOffset expires, IEnumerable<Claim> claims)
        {
            // Create a signed JWT
            var securityToken = new JwtSecurityToken(
                issuer: Options.JwtIssuer,
                audience: Options.JwtAudience,
                claims: claims,
                expires: expires.UtcDateTime, // Stupid library uses DateTime
                signingCredentials: _signingCredentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return jwt;
        }

        /// <summary>
        /// Validates a JWT.
        /// </summary>
        /// <param name="jwt">The JWT to validate.</param>
        /// <returns>A list of claims defined by a valid JWT.</returns>
        public IEnumerable<Claim> ValidateToken(string jwt)
        {
            // Validate the JWT
            var validationParams = new TokenValidationParameters
            {
                // The most important one
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,

                // The second most important one
                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateIssuer = true,
                ValidIssuer = Options.JwtIssuer,

                ValidateAudience = true,
                ValidAudience = Options.JwtAudience,
            };

            ClaimsPrincipal principal;
            var tokenHandler = new JwtSecurityTokenHandler();

            // Change the default behavior of mapping 'sub' to name and keep 'sub' as 'sub'
            tokenHandler.InboundClaimTypeMap[JwtTokenClaims.Sub] = JwtTokenClaims.Sub;

            // This will throw an exception if invalid
            principal = tokenHandler.ValidateToken(jwt, validationParams, out SecurityToken validatedToken);
            var claims = principal.Claims.ToList();

            return claims;
        }
    }
}
