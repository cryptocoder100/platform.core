namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.Text.Json.Serialization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Extension methods for the <see cref="JsonOptions" /> class.
    /// </summary>
    public static class JsonOptionsExtensions
    {
        /// <summary>
        /// Configures the <see cref="JsonOptions" /> with platform defaults with regards to casing, null-handling, and indenting.
        /// </summary>
        /// <param name="options">The <see cref="JsonOptions" /> to modify.</param>
        /// <param name="environment">An <see cref="IWebHostEnvironment" /> instance.</param>
        /// <returns>The <see cref="JsonOptions"/>.</returns>
        public static JsonOptions AddPlatformDefaults(this JsonOptions options, IWebHostEnvironment environment)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            // Serialize enums as camelCase strings
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter(options.JsonSerializerOptions.PropertyNamingPolicy));

            // Don't print null values
            // options.JsonSerializerOptions.IgnoreNullValues = true; // Obsolete in .net6 replaced with below condition
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // Debug-friendly format
            if (environment.IsDevelopment())
            {
                options.JsonSerializerOptions.WriteIndented = true;
            }

            return options;
        }
    }
}
