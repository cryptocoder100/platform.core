namespace Exos.Platform.ICTLibrary.Core.Extension
{
    using System;
    using Exos.Platform.ICTLibrary.Repository;
    using Exos.Platform.ICTLibrary.Repository.Model;
    using Exos.Platform.Messaging.Core;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Service Collection Extension to Configure ICT.
    /// </summary>
    public static class ServiceCollectionIctExtension
    {
        /// <summary>
        ///  Configures ICT given a ConnectionStrings configuration section.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="configuration">Include: "MessageDb", "IctDb".</param>
        /// <returns>Configured services.</returns>
        public static IServiceCollection ConfigureIct(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Adding the dependency needed for ICT ,Not using poll since entity framework has issue with pooling
            // and multi-threading. Below statement is scoped so the context has to be scoped
            services.Configure<MessagePublisherOptions>(configuration.GetSection("Messaging:MessagePublisherOptions"));
            services.Configure<MessageSection>(configuration.GetSection("Messaging"));
            services.AddDbContext<IctContext>(options =>
                options.UseSqlServer(configuration.GetValue<string>("SQL:ICTReadWriteConnectionString")));
            services.AddTransient<IctContext, IctContext>();
            services.AddTransient<IIctEventPublisher, IctEventPublisher>();
            services.AddScoped<IIctRepository, IctRepository>();
            services.AddSingleton<IExosMessaging, ExosMessaging>();
            return services;
        }
    }
}
