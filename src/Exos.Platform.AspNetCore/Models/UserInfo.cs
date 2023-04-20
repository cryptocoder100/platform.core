namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// User Info.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Gets or sets UserId.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Gets or sets UserName.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets FirstName.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets LastName.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets UserType.
        /// </summary>
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets TenantHierarchyId.
        /// </summary>
        public string TenantHierarchyId { get; set; }
    }
}
