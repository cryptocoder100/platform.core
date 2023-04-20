#pragma warning disable CA1307 // Specify StringComparison

namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using HashidsNet;

    /// <summary>
    /// Extension methods for the Guid class.
    /// </summary>
    public static class GuidExtensions
    {
        /// <summary>
        /// Converts a HEX format GUID to a short, user-friendly alpha-numeric string.
        /// </summary>
        /// <param name="guidValue">Guid object.</param>
        /// <returns>Short Guid string.</returns>
        [Obsolete("Use a plain GUID instead.")]
        public static string ToShortId(this Guid guidValue)
        {
            var hashids = new Hashids();
            return hashids.EncodeHex(guidValue.ToString().Replace("-", string.Empty));
        }
    }
}
#pragma warning restore CA1307 // Specify StringComparison