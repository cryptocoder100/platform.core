#pragma warning disable CA1715 // Identifiers should have correct prefix
namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using Exos.Platform.Persistence.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Add the ChangeTrackingService and its settings to dependency injection.
    /// </summary>
    public static class EventTrackingExtensions
    {
        /// <summary>
        /// Configure Event Tracking service.
        /// </summary>
        /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
        /// <param name="services">Service Collection.</param>
        /// <param name="configuration">Configuration properties.</param>
        public static void AddEventTrackingService<T>(this IServiceCollection services, IConfiguration configuration) where T : EventTrackingEntity, new()
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Change/Event Tracking
            services.AddScoped<IEventTrackingService, EventTrackingService<T>>();
            services.AddScoped<IEventSqlServerRepository<T>, EventSqlServerRepository<T>>();

            // Archival Repository
            services.AddScoped<IEventArchivalSqlServerRepository<T>>(factory => factory.GetService<EventArchivalSqlServerRepository<T>>());
            services.AddDbContext<EventArchivalSqlServerRepository<T>>(eventOptions =>
            {
                eventOptions.UseSqlServer(configuration.GetConnectionString("EventArchivalSqlServerRepository"));
            });

            services.Configure<FutureEventsConfig>(configuration.GetSection("FutureEventsConfig"));
            services.Configure<ServiceConfig>(configuration.GetSection("ServiceConfig"));
        }

        /// <summary>
        /// Configure Event Tracking Servirce.
        /// </summary>
        /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
        /// <typeparam name="TCP"><see cref="EventPublishCheckPointEntity"/>.</typeparam>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        public static void AddEventTrackingService<T, TCP>(this IServiceCollection serviceCollection, IConfiguration configuration)
            where T : EventTrackingEntity, new()
            where TCP : EventPublishCheckPointEntity, new()
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Change Event Tracking
            serviceCollection.AddScoped<IEventTrackingService, EventTrackingService<T>>();
            serviceCollection.AddScoped<IEventSqlServerRepository<T>, EventSqlServerRepository<T>>();
            serviceCollection.AddScoped<IEventPublishCheckPointSqlRepository<TCP>, EventPublishCheckPointSqlServerRepository<TCP>>();

            // Archival Repository
            serviceCollection.AddScoped<IEventArchivalSqlServerRepository<T>>(factory => factory.GetService<EventArchivalSqlServerRepository<T>>());
            serviceCollection.AddDbContext<EventArchivalSqlServerRepository<T>>(eventOptions =>
            {
                eventOptions.UseSqlServer(configuration.GetConnectionString("EventArchivalSqlServerRepository"));
            });

            serviceCollection.Configure<FutureEventsConfig>(configuration.GetSection("FutureEventsConfig"));
            serviceCollection.Configure<ServiceConfig>(configuration.GetSection("ServiceConfig"));
        }
    }
}
#pragma warning restore CA1715 // Identifiers should have correct prefix