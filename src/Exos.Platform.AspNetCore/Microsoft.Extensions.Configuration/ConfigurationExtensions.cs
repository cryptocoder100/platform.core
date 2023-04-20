#pragma warning disable CA1031 // Do not catch general exception types
namespace Microsoft.Extensions.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;

    /// <summary>
    /// Extension methods for working with <see cref="IConfiguration" /> instances.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Tries to extract the value with the specified <paramref name="key" /> and converts it to type <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <param name="defaultValue">The default value to use if no value is found.</param>
        /// <returns>The converted value if found; otherwise, <paramref name="defaultValue" />.</returns>
        public static T TryGetValue<T>(this IConfiguration configuration, string key, T defaultValue)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(key);
            var value = section.Value;
            if (value != null && TryConvertValue(typeof(T), value, key, out var result, out var error) && error == null)
            {
                return (T)result;
            }

            return defaultValue;
        }

        // Comes straight out of the dotnet source
        private static bool TryConvertValue(Type type, string value, string path, out object result, out Exception error)
        {
            error = null;
            result = null;
            if (type == typeof(object))
            {
                result = value;
                return true;
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(value))
                {
                    return true;
                }

                return TryConvertValue(Nullable.GetUnderlyingType(type), value, path, out result, out error);
            }

            var converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(value);
                }
                catch (Exception ex)
                {
                    error = new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Failed to convert configuration value at '{0}' to type '{1}'.", path, type), ex);
                }

                return true;
            }

            return false;
        }
    }
}

// References:
// https://github.com/dotnet/extensions/blob/77d62c5738da86985cf69ba0479539b6fdbc904e/src/Configuration/Config.Binder/src/ConfigurationBinder.cs
