namespace Exos.Platform.TenancyHelper.MultiTenancy
{
    /// <summary>
    /// Entity Policy Attributes.
    /// </summary>
    public class EntityPolicyAttributes
    {
        /// <summary>
        /// Gets or sets a value indicating whether the Entity requires Multi-Tenancy.
        /// </summary>
        public bool IsEntityMultiTenant { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Policy is stored in cache.
        /// </summary>
        public bool IsCacheable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Servicer Filter for Vendor Tenant is required..
        /// </summary>
        public bool ApplyServicerFilterForVendorTenant { get; set; }
    }
}
