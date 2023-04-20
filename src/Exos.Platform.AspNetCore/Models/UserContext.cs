#pragma warning disable CA1812 // Avoid uninstantiated internal classes
namespace Exos.Platform.AspNetCore.Models
{
    // $/FieldServices/SpaLogin/Dev4_EXOS_RC1/src/RestWebAPIs/Login.RestWebAPIs/Controllers/CustomAuthController.cs

    /// <summary>
    /// User Context.
    /// </summary>
    internal class UserContext
    {
        /// <summary>
        /// Gets or sets UserId.
        /// </summary>
        public int UserId { get; set; }

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
        /// Gets or sets a value indicating whether UserContext isActive.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets UserType.
        /// </summary>
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets SessionId.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets Token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets ServicerTenantId.
        /// </summary>
        public long? ServicerTenantId { get; set; }
    }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes