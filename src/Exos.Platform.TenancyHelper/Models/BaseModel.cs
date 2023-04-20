#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter
#pragma warning disable CA1707 // Identifiers should not contain underscores
namespace Exos.Platform.TenancyHelper.Models
{
    using System.Text.Json.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Base class to be implemented in each Document Model.
    /// </summary>
    public abstract class BaseModel
    {
        /// <summary>
        /// Gets the CosmosDocType field.
        /// </summary>
        public abstract string CosmosDocType { get; }

        /// <summary>
        /// Gets or sets the Document Id field.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Version field.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the tenancy fields.
        /// </summary>
        public TenantModel Tenant { get; set; }

        /// <summary>
        /// Gets or sets the Audit Fields.
        /// </summary>
        public AuditModel Audit { get; set; }

        /// <summary>
        /// Gets or sets _Etag value.
        /// Azure Cosmos DB uses ETags for handling optimistic concurrency.
        /// </summary>
        [JsonProperty("_etag")]
        [JsonPropertyName("_etag")]
        public string _Etag { get; set; }
    }
}
#pragma warning restore SA1300 // ElementMustBeginWithUpperCaseLetter
#pragma warning restore CA1707 // Identifiers should not contain underscores