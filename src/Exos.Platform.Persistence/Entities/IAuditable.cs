#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.Persistence.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Entities should implement this interface to allow the set of audit information and concurrency field.
    /// </summary>
    public interface IAuditable
    {
        /// <summary>
        /// Gets or sets a value indicating whether the Entity is active or not, usually set to false in delete method (soft delete).
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the value of when the entity was created.
        /// </summary>
        DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the value of the user that creates the entity.
        /// </summary>
        string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the value of when the entity was updated for the last time.
        /// </summary>
        DateTime? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the value of the user that update the entity for the last time.
        /// </summary>
        string LastUpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the value of the version of the entity, to handle concurrency conflicts.
        /// </summary>
        [Timestamp]
        byte[] Version { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays