namespace Exos.Platform.ICTLibrary.Repository.Model
{
    using System;
    using Microsoft.EntityFrameworkCore;

    /// <inheritdoc/>
    public class IctContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IctContext"/> class.
        /// </summary>
        public IctContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IctContext"/> class.
        /// </summary>
        /// <param name="options">DbContextOptions.</param>
        public IctContext(DbContextOptions<IctContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the EventEntityTopic Entity.
        /// </summary>
        public virtual DbSet<EventEntityTopic> EventEntityTopic { get; set; }

        /// <summary>
        /// Gets or sets the EventTracking Entity.
        /// </summary>
        public virtual DbSet<EventTracking> EventTracking { get; set; }

        /// <summary>
        /// Gets or sets the EventTracking EventPublisher.
        /// </summary>
        public virtual DbSet<EventPublisher> EventPublisher { get; set; }

        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder != null && !optionsBuilder.IsConfigured)
            {
                // optionsBuilder.UseSqlServer("Server=AZDEV-SQL-02;Database=PlatformDB_poc;Trusted_Connection=false;user id = azdeveloper; password = Password1");
            }
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.Entity<EventEntityTopic>(entity =>
            {
                entity.ToTable("EventEntityTopic", "ict");

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EntityName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EntityNameDescription)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.EventName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EventNameDescrption)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TopicName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Version).IsRowVersion();

                entity.HasOne(d => d.Publisher)
                    .WithMany(p => p.EventEntityTopic)
                    .HasForeignKey(d => d.PublisherId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_EventEntityTopic_EventPublisher");
            });

            modelBuilder.Entity<EventPublisher>(entity =>
            {
                entity.ToTable("EventPublisher", "ict");

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EventPublisherDescription)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EventPublisherName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Version)
                    .HasColumnName("version")
                    .IsRowVersion();
            });

            modelBuilder.Entity<EventTracking>(entity =>
            {
                entity.ToTable("EventTracking", "ict");

                entity.Property(e => e.ApplicationName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TopicName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EntityName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.EventName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TrackingId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Version).IsRowVersion();
            });
        }
    }
}
