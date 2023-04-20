namespace Exos.Platform.TenancyHelper.Models
{
#pragma warning disable SA1300 // Elements in enum must begin with Upper Case. Not changed backwards compatibility.
    /// <summary>
    /// User Tenant Type.
    /// </summary>
    public enum TenantType
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
#pragma warning restore SA1300 // ElementMustBeginWithUpperCaseLetter
}
