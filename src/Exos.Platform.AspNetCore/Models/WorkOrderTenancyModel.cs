using System;
using Exos.Platform.AspNetCore.Entities;

namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// Work Order Tenancy Model.
    /// </summary>
    /// <seealso cref="WorkOrderTenantEntity" />
    public class WorkOrderTenancyModel
    {
        /// <summary>
        /// Gets or sets ServicerTenantId.
        /// </summary>
        public long? ServicerTenantId { get; set; }

        // Following section is dedicated to workorder tenancy.

        /// <summary>
        /// Gets or sets ServicerGroupTenantId.
        /// </summary>
        public long? ServicerGroupTenantId { get; set; }

        /// <summary>
        /// Gets or sets VendorTenantId.
        /// </summary>
        public long? VendorTenantId { get; set; }

        /// <summary>
        /// Gets or sets SubContractorTenantId.
        /// </summary>
        public long? SubContractorTenantId { get; set; }

        /// <summary>
        /// Gets or sets ClientTenantId.
        /// </summary>
        public long? ClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets SubClientTenantId.
        /// </summary>
        public long? SubClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets WorkOrderId.
        /// </summary>
        public long? WorkOrderId { get; set; }

        /// <summary>
        /// Gets or sets Source System WorkOrderNumber.
        /// </summary>
        public string SourceSystemWorkOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets Source System OrderNumber.
        /// </summary>
        public string SourceSystemOrderNumber { get; set; }
    }
}
