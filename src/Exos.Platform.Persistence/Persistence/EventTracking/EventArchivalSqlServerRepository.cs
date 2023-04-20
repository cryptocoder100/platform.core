namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Persistence.EventPoller;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository to archive events.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public class EventArchivalSqlServerRepository<T> : DbContext, IEventArchivalSqlServerRepository<T> where T : EventTrackingEntity
    {
        private readonly ILogger<EventArchivalSqlServerRepository<T>> _logger;

        private readonly EventPollerServiceSettings _eventPollerServiceSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArchivalSqlServerRepository{T}"/> class.
        /// </summary>
        /// <param name="options">DBContext Repository options.</param>
        /// <param name="eventPollerServiceSettings">Service settings from configuration file.</param>
        /// <param name="logger">Logger instance.</param>
        public EventArchivalSqlServerRepository(DbContextOptions<EventArchivalSqlServerRepository<T>> options, IOptions<EventPollerServiceSettings> eventPollerServiceSettings, ILogger<EventArchivalSqlServerRepository<T>> logger) : base(options)
        {
            if (eventPollerServiceSettings == null)
            {
                throw new ArgumentNullException(nameof(eventPollerServiceSettings));
            }

            _eventPollerServiceSettings = eventPollerServiceSettings.Value ?? throw new ArgumentNullException(nameof(eventPollerServiceSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets or sets EventTracking Entity.
        /// </summary>
        public DbSet<T> EventTrackingEntities { get; set; }

        /// <inheritdoc/>
        public async Task<int> ArchiveEvents(List<T> eventTrackingEntryList)
        {
            if (eventTrackingEntryList == null)
            {
                throw new ArgumentNullException(nameof(eventTrackingEntryList));
            }

            foreach (var eventTrackingEntry in eventTrackingEntryList)
            {
                eventTrackingEntry.Version = null;
            }

            await AddRangeAsync(eventTrackingEntryList).ConfigureAwait(false);
            var updatedRows = await SaveChangesAsync().ConfigureAwait(false);
            return updatedRows;
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<T>(entity =>
            {
                entity.Property(e => e.Version).IsRowVersion();
                entity.ToTable(_eventPollerServiceSettings.ArchivalTable, _eventPollerServiceSettings.ArchivalSchema);
            });
        }
    }
}
