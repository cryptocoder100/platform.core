#pragma warning disable SA1649
namespace Exos.Platform.TenancyHelper.Interfaces
{
    using Exos.Platform.TenancyHelper.Models;

    // For testing its here but will be moved to Common/Shared libs
    /*public interface IBaseTenantModel
    {
        int SubClient { get; set; }
        int Vendor { get; set; }
        int SubContractor { get; set; }
        int Servicer { get; set; }
        int ServicerGroup { get; set; }
    }*/

    /// <summary>
    /// Interface to declare the multi-tenancy fields.
    /// </summary>
    public interface ITenant
    {
        /// <summary>
        /// Gets or sets Tenant Model.
        /// </summary>
        TenantModel Tenant { get; set; }
    }
}
#pragma warning restore SA1649