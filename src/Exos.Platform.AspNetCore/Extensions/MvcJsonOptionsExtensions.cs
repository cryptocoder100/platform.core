namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Extension methods for the MvcJsonOptions class.
    /// </summary>
    public static class MvcJsonOptionsExtensions
    {
        /// <summary>
        /// Adjusts the default MVC JSON serialization to be consistent with platform defaults with regard to casing, null-handling, and indenting.
        /// </summary>
        /// <param name="options">The MvcJsonOptions to configure.</param>
        /// <param name="services">An instance of the IServiceCollection to use for checking environment configurations.</param>
        /// <returns>The updated MvcJsonOptions.</returns>
        [Obsolete("Use AddPlatformDefaults(MvcNewtonsoftJsonOptions, IWebHostEnvironment)")]
        public static MvcNewtonsoftJsonOptions AddPlatformDefaults(this MvcNewtonsoftJsonOptions options, IServiceCollection services)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Serialize enums as camelCase strings
            options.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

            // Don't print null values
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // Debug-friendly format
            var env = services.BuildServiceProvider().GetService<IWebHostEnvironment>();
            if (env != null && env.IsDevelopment())
            {
                options.SerializerSettings.Formatting = Formatting.Indented;
            }

            return options;
        }

        /// <summary>
        /// Configures the <see cref="MvcJsonOptionsExtensions" /> with platform defaults with regards to casing, null-handling, and indenting.
        /// </summary>
        /// <param name="options">The <see cref="MvcJsonOptionsExtensions" /> to modify.</param>
        /// <param name="environment">An <see cref="IWebHostEnvironment" /> instance.</param>
        /// <returns>The <see cref="MvcNewtonsoftJsonOptions"/>.</returns>
        public static MvcNewtonsoftJsonOptions AddPlatformDefaults(this MvcNewtonsoftJsonOptions options, IWebHostEnvironment environment)
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
            options.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

            // Don't print null values
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // Debug-friendly format
            if (environment.IsDevelopment())
            {
                options.SerializerSettings.Formatting = Formatting.Indented;
            }

            return options;
        }
    }
}
