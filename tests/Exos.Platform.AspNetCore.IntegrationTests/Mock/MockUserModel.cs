#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable CA1822 // Mark members as static
namespace Exos.Platform.AspNetCore.IntegrationTests.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class MockUserModel
    {
        public string Id { get; set; }

        public string Version { get; set; }

        // public TenantModel Tenant { get; set; }
        // public AuditModel Audit { get; set; }
        [JsonPropertyName("_etag")]

        public string _Etag { get; set; }

        public string CosmosDocType => "User";

        public string Username { get; set; }

        /// <summary>Gets or sets user First Name.</summary>
        public string FirstName { get; set; }

        /// <summary>Gets or sets user Last Name.</summary>
        public string LastName { get; set; }

        /// <summary>Gets or sets a value indicating whether user Exos Admin.</summary>
        public bool ExosAdmin { get; set; }

        /// <summary>Gets or sets user Tenant Type.</summary>
        public string TenantType { get; set; }

        /// <summary>Gets or sets user Tenant Id.</summary>
        public long TenantId { get; set; }

        /// <summary>Gets or sets user Tenant Ids.</summary>
        public List<long> TenantIds { get; set; }

        /// <summary>Gets or sets user Servicer Associations.</summary>
        public List<long> ServicerAssociations { get; set; }

        /// <summary>Gets or sets user Division Associations.</summary>
        public List<int> DivisionAssociations { get; set; }

        /// <summary>Gets or sets a value indicating whether user Is Active.</summary>
        public bool IsActive { get; set; }

        /// <summary>Gets or sets user Status.</summary>
        public string Status { get; set; }

        /// <summary>Gets or sets user Last Login Date.</summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>Gets or sets user Client Secret.</summary>
        public string ClientSecret { get; set; }

        /// <summary>Gets or sets user Lines of Business.</summary>
        public List<int> LinesOfBusiness { get; set; }

        /// <summary>Gets or sets user Business Functions.</summary>
        public List<string> BusinessFunctions { get; set; }

        /// <summary>Gets or sets user Granted Resources.</summary>
        public List<string> GrantedResources { get; set; }

        /// <summary>Gets or sets user Granted Roles.</summary>
        public List<string> GrantedRoles { get; set; }

        public List<string> AddlGrantedRoles { get; set; }

        /// <summary>Gets or sets user Servicer Groups.</summary>
        public Dictionary<string, List<long>> ServicerGroups { get; set; }

        /// <summary>Gets or sets user UI Resources.</summary>
        public List<string> UIResources { get; set; } = new List<string>();

        /// <summary>Gets or sets user Api Resources.</summary>
        public List<string> ApiResources { get; set; } = new List<string>();

        public string NavigationPath { get; set; }

        /// <summary>Gets or sets user Phone.</summary>
        public string Phone { get; set; }

        /// <summary>Gets or sets user Email.</summary>
        public string Email { get; set; }

        /// <summary>Gets or sets user signature.</summary>
        public string Signature { get; set; }

        /// <summary>Gets or sets a value indicating whether user is pending activation.</summary>
        public bool PendingActivation { get; set; }

        /// <summary>Gets or sets user application id.</summary>
        public long AppUserId { get; set; }

        /// <summary>Gets or sets a value indicating whether user is manager.</summary>
        public bool IsManager { get; set; }

        public bool IsCorporateUser { get; set; }

        /// <summary>Gets or sets a value indicating whether user Team class.</summary>
        // public List<TeamType> Teams { get; set; }
        public bool IsMigratedToB2c { get; set; }

        public string ManagerId { get; set; }

        /// <summary>
        /// Gets or sets operationalType.
        /// </summary>
        public List<int> OperationalType { get; set; }
    }
}
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1822 // Mark members as static