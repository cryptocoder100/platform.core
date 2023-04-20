namespace Exos.Platform.TenancyHelper.Models
{
    using System;

    /// <summary>
    /// Model that represent the Audit Fields,
    /// fields are used to track document creation, last update and deletion.
    /// </summary>
    public class AuditModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether document is Deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a date-time indicating when the document was created.
        /// </summary>
        public DateTimeOffset CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the user that creates the document.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets a date-time indicating when the document was updated.
        /// </summary>
        public DateTimeOffset? LastUpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the user that did the last update of the document.
        /// </summary>
        public string LastUpdatedBy { get; set; }
    }
}
