namespace Exos.Platform.AspNetCore.Security
{
    using System.IdentityModel.Tokens.Jwt;

    /// <summary>
    /// Common JWT claims.
    /// </summary>
    public static class JwtTokenClaims
    {
        /// <summary>
        /// Gets the 'sub' claim (i.e. subject, user ID)..
        /// </summary>
        public static string Sub { get; } = JwtRegisteredClaimNames.Sub;

        /// <summary>
        /// Gets the 'at_hash' claim (i.e. token hash, browser fingerprint)..
        /// </summary>
        public static string AtHash { get; } = JwtRegisteredClaimNames.AtHash;

        /// <summary>
        /// Gets the 'iss' claim (i.e. issuer)..
        /// </summary>
        public static string Iss { get; } = JwtRegisteredClaimNames.Iss;

        /// <summary>
        /// Gets the 'aud' claim (i.e. audience)..
        /// </summary>
        public static string Aud { get; } = JwtRegisteredClaimNames.Aud;

        /// <summary>
        /// Gets the 'name' claim (i.e. full name)..
        /// </summary>
        public static string Name { get; } = "name";

        /// <summary>
        /// Gets the 'iat' claim (i.e. issued at)..
        /// </summary>
        public static string Iat { get; } = JwtRegisteredClaimNames.Iat;

        /// <summary>
        /// Gets the 'jti' claim (i.e. JWT ID)..
        /// </summary>
        public static string Jti { get; } = JwtRegisteredClaimNames.Jti;

        /// <summary>
        /// Gets the 'exp' claim (i.e. expiration time)..
        /// </summary>
        public static string Exp { get; } = JwtRegisteredClaimNames.Exp;
    }
}
