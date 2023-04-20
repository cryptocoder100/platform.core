namespace Exos.Platform.TenancyHelper.Entities
{
    /// <summary>
    /// Interface to declare the multi-tenancy fields, needs to be implemented by
    /// each EF entity that requires multi-tenancy.
    /// </summary>
    public interface ITenant
    {
        /// <summary>
        /// Gets EntityName.
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// Gets or sets ClientTenantId.
        /// </summary>
        int ClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets SubClientTenantId.
        /// </summary>
        int SubClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets VendorTenantId.
        /// </summary>
        int VendorTenantId { get; set; }

        /// <summary>
        /// Gets or sets SubContractorTenantId.
        /// </summary>
        int SubContractorTenantId { get; set; }

        /// <summary>
        /// Gets or sets ServicerTenantId.
        /// </summary>
        short ServicerTenantId { get; set; }

        /// <summary>
        /// Gets or sets ServicerGroupTenantId.
        /// </summary>
        short ServicerGroupTenantId { get; set; }
    }
}
