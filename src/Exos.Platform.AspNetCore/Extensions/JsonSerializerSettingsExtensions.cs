namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Extension methods for the JsonSerializerSettings class.
    /// </summary>
    public static class JsonSerializerSettingsExtensions
    {
        /// <summary>
        /// Adjusts the default JSON serialization to be consistent with platform defaults with regard to casing, null-handling, enums, and indenting.
        /// </summary>
        /// <param name="settings">The JsonSerializerSettings to configure.</param>
        /// <param name="services">An instance of the IServiceCollection to use for checking environment configurations.</param>
        /// <returns>The updated JsonSerializerSettings.</returns>
        [Obsolete("Use AddPlatformDefaults(JsonSerializerSettings, IWebHostEnvironment)")]
        public static JsonSerializerSettings AddPlatformDefaults(this JsonSerializerSettings settings, IServiceCollection services)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Match the default behavior for MVC serialization
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

            var env = services.BuildServiceProvider().GetService<IWebHostEnvironment>();
            if (env != null && env.IsDevelopment())
            {
                settings.Formatting = Formatting.Indented;
            }

            return settings;
        }

        /// <summary>
        /// Configures the default JSON serialization to be consistent with platform defaults with regards to casing, null-handling, and indenting.
        /// </summary>
        /// <param name="settings">The <see cref="JsonSerializerSettings" /> to configure.</param>
        /// <param name="environment">An <see cref="IWebHostEnvironment"/> instance.</param>
        /// <returns>The <see cref="JsonSerializerSettings"/>.</returns>
        public static JsonSerializerSettings AddPlatformDefaults(this JsonSerializerSettings settings, IWebHostEnvironment environment)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            // Match the default behavior for MVC serialization
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

            if (environment.IsDevelopment())
            {
                settings.Formatting = Formatting.Indented;
            }

            return settings;
        }
    }
}
