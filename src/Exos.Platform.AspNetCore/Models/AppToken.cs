#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Exos.Platform.AspNetCore.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Application Token.
    /// </summary>
    [Serializable]
    internal class AppToken
    {
        /// <summary>
        /// Gets or sets the Expires.
        /// </summary>
        public DateTime Expires { get; set; }

        /// <summary>
        /// Gets or sets the UserId.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the UserName.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the FirstName.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the LastName.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the AppToken IsActive.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the UserType.
        /// </summary>
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets the TenantHierarchyId.
        /// </summary>
        public string TenantHierarchyId { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public int? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the WorkOrderIds.
        /// This contains consumer related work order as consumer logs in with system account, storing it here with user context.
        /// </summary>
        public List<string> WorkOrderIds { get; set; }
    }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes