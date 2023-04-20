#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// Supported Authentication schemes.
    /// </summary>
    public enum PlatformAuthScheme
    {
        /// <summary>
        /// Exos Authentication Token.
        /// </summary>
        ExosAuthToken,

        /// <summary>
        /// Azure Active Directory.
        /// </summary>
        AzureAD,

        /// <summary>
        /// Exos Cookies.
        /// </summary>
        ExosCookies,

        /// <summary>
        /// Api Key/Secret.
        /// </summary>
        ApiKey,

        /// <summary>
        /// Bearer Token.
        /// </summary>
        Bearer,

        /// <summary>
        /// JSON Web Token.
        /// </summary>
        Jwt,
    }

    /// <summary>
    /// Claim Constants.
    /// </summary>
    public static class ClaimConstants
    {
        /// <summary>
        /// Original Authorization Scheme Name.
        /// </summary>
        public static readonly string OriginalAuthSchemeName = "originalauthschemename";

        /// <summary>
        /// Elevated Right Claim name.
        /// </summary>
        public static readonly string ElevatedRightClaimName = "elevatedright";

        /// <summary>
        /// Access token for JWT via BasicAuth.
        /// </summary>
        public static readonly string ApiKeyAccessToken = "apikeyaccesstoken";
    }
}
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName