#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.TenancyHelper.Utils
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Hash a string using MD5 algorithm.
    /// </summary>
    public static class Md5Hash
    {
        /// <summary>
        /// Hash a string using MD5 algorithm.
        /// </summary>
        /// <param name="strkeyToHash">String to Hash.</param>
        /// <returns>Hashed string.</returns>
        public static string GetHash(string strkeyToHash)
        {
            using (var cryptoServiceProvider = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(strkeyToHash);
                var builder = new StringBuilder();

                bytes = cryptoServiceProvider.ComputeHash(bytes);

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                return builder.ToString();
            }
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase
