using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Exos.Platform.AspNetCore.Authentication
{
    /// <summary>
    /// Manage how the token is revoked in the system.
    /// </summary>
    public static class TokenRevoker
    {
        /// <summary>
        /// Check if the token has been revoked (blacklisted in distributedCache).
        /// </summary>
        /// <param name="token">accessToken.</param>
        /// <param name="distributedCache">distributedCache.</param>
        /// <returns>IsTokenRevoked. </returns>
        public static async Task<bool> IsTokenRevoked(string token, IDistributedCache distributedCache)
        {
            var isRevoked = false;
            var key = GetRevokeKey(token);
            var isRevokedData = await distributedCache.GetStringAsync(key).ConfigureAwait(false);

            if (isRevokedData != null)
            {
                isRevoked = JsonSerializer.Deserialize<bool>(isRevokedData);
            }

            return isRevoked;
        }

        /// <summary>
        /// Revoke a token by blacklisting it in distributedCache.
        /// </summary>
        /// <param name="token">accessToken.</param>
        /// <param name="distributedCache">distributedCache.</param>
        public static async Task RevokeToken(string token, IDistributedCache distributedCache)
        {
            var key = GetRevokeKey(token);
            await distributedCache.SetStringAsync(
                key, System.Text.Json.JsonSerializer.Serialize(true)).ConfigureAwait(false);
        }

        private static string GetRevokeKey(string token)
        {
            return $"IsRevokedTokenKey: {token}";
        }
    }
}
