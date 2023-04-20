namespace Exos.Platform.AspNetCore.Extensions
{
    using System;

    /// <summary>
    /// Extension methods for the String class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a copy of the string in camelCase.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <returns>String in camelCase format.</returns>
        public static string ToCamelCaseInvariant(this string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            str = char.ToLowerInvariant(str[0]) + str.Substring(1);
            return str;
        }
    }
}
