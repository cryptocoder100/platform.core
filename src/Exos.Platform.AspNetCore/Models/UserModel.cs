#pragma warning disable CA1812 // Avoid uninstantiated internal classes
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    // See Exos.UserSvc.Models.UserModel

    /// <summary>
    /// User Model.
    /// </summary>
    internal class UserModel
    {
        /// <summary>
        /// Gets or sets Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Model.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets Username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets FirstName.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets LastName.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets TenantType.
        /// </summary>
        public string TenantType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is ExosAdmin.
        /// </summary>
        public bool ExosAdmin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsManager.
        /// </summary>
        public bool IsManager { get; set; }

        /// <summary>
        /// Gets or sets TenantId.
        /// </summary>
        public long TenantId { get; set; }

        /// <summary>
        /// Gets or sets TenantIds.
        /// This is for exos admin functionality. This user can serve multiple servicer/tenants.
        /// </summary>
        public List<long> TenantIds { get; set; }

        /// <summary>
        /// Gets or sets ClientSecret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets LinesOfBusiness.
        /// </summary>
        public List<string> LinesOfBusiness { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets BusinessFunctions.
        /// </summary>
        public List<string> BusinessFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Gets ServicerGroups.
        /// </summary>
        public Dictionary<string, List<long>> ServicerGroups { get; } = new Dictionary<string, List<long>>();

        /// <summary>
        /// Gets or sets UIResources.
        /// </summary>
        public List<string> UIResources { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets ApiResources.
        /// </summary>
        public List<string> ApiResources { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets Teams.
        /// </summary>
        public List<TeamType> Teams { get; set; }

        /// <summary>
        /// Gets or sets OperationalType.
        /// </summary>
        public List<int> OperationalType { get; set; }

        public SubTenantType? SubTenantType { get; set; }

        /// <summary>
        ///  Gets or sets AssociatedSubClients.
        /// </summary>
        public List<AssociatedClient> AssociatedSubClients { get; set; }
    }

    /// <summary>
    /// Sub Tenant Type.
    /// </summary>
    public enum SubTenantType
    {
        /// <summary>   An enum constant representing the none= 0 option. </summary>
        None = 0,

        /// <summary>   An enum constant representing the loanofficer option. </summary>
        loanofficer = 1,

        /// <summary>   An enum constant representing the realtor option. </summary>
        realtor = 2,

        /// <summary>   An enum constant representing the underwriter option. </summary>
        underwriter = 3,

        /// <summary>   An enum constant representing the restricted master client. </summary>
        restrictedmasterclient = 4,

        /// <summary>   An enum constant representing the restricted client user. </summary>
        restrictedclient = 5
    }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
#pragma warning restore SA1201 // Elements should appear in the correct order
#pragma warning restore SA1300 // Element should begin with upper-case letter