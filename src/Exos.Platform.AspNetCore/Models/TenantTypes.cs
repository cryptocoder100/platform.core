namespace Exos.Platform.AspNetCore.Models
{
#pragma warning disable SA1300 // Element should begin with upper-case letter
    /// <summary>
    /// User Tenant Type.
    /// </summary>
    public enum TenantTypes
    {
        /// <summary>
        /// Servicer Tenant Type.
        /// </summary>
        servicer,

        /// <summary>
        /// Vendor Tenant Type.
        /// </summary>
        vendor,

        /// <summary>
        ///  Subcontractor Tenant Type.
        /// </summary>
        subcontractor,

        /// <summary>
        ///  Master-Client Tenant Type.
        /// </summary>
        masterclient,

        /// <summary>
        ///  Sub-Client Tenant Type.
        /// </summary>
        subclient,

        /// <summary>
        ///  Exos Tenant Type.
        /// </summary>
        exos,
    }
#pragma warning restore SA1300 // Element should begin with upper-case letter
}
