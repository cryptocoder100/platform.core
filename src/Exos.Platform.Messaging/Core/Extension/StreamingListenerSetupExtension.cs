namespace Exos.Platform.Messaging.Core.Extension
{
    using System;
    using Exos.Platform.Messaging.Core.StreamListener;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// StreamingListenerSetupExtension.
    /// </summary>
    public static class StreamingListenerSetupExtension
    {
        /// <summary>
        /// Gets or sets ServiceProvider.
        /// </summary>
        public static ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets Configuration.
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// ConfigureExosEventHubListener.
        /// </summary>
        /// <param name="services">services.</param>
        /// <param name="configuration">configuration.</param>
        /// <returns>Service Collection object.</returns>
        public static IServiceCollection ConfigureExosEventHubListener(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<EventingSection>(configuration.GetSection("ExosStreaming"));
            services.AddSingleton<IExosStreamingListener, ExosStreamingListener>(); // This is the package to listen api
            return services;
        }
    }
}
