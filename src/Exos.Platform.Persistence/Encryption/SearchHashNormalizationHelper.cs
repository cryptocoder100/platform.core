namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Central helper class to handle normalizing the data for search hashing.
    /// </summary>
    public static class SearchHashNormalizationHelper
    {
        private static readonly CultureInfo CultureInfo = new CultureInfo("en-US", false);

        /// <summary>
        /// Normalizes the input based on the strategy.
        /// </summary>
        /// <param name="value">value to hash.</param>
        /// <param name="strategy">strategy to use.</param>
        /// <returns>the value.</returns>
        /// <exception cref="InvalidOperationException">thrown if you didn't pass an appropriate strategy.</exception>
        public static string Normalize(string value, SearchHashNormalizationStrategy strategy)
        {
            if (string.IsNullOrEmpty(value) || (strategy == SearchHashNormalizationStrategy.None))
            {
                return value;
            }

            switch (strategy)
            {
                case SearchHashNormalizationStrategy.Name:
                    value = Regex.Replace(value, "[^a-zA-Z0-9]", string.Empty);
                    value = value.Trim();
                    value = value.ToLower(CultureInfo);
                    break;
                case SearchHashNormalizationStrategy.Email:
                    value = value.Trim();
                    value = value.ToLower(CultureInfo);
                    break;
                case SearchHashNormalizationStrategy.Phone:
                    value = value.Trim();
                    break;
                default:
                    throw new InvalidOperationException("Unknown normalization strategy");
            }

            return value;
        }
    }
}
