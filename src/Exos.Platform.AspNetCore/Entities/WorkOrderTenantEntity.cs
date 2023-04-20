using System;
using System.Collections.Generic;
using System.Text;

namespace Exos.Platform.AspNetCore.Entities
{
    // See also WorkOrderTenancyModel.
    // Our entity is used with header strings and so we don't try convert to more specific types.
    internal sealed class WorkOrderTenantEntity
    {
        public string ServicerGroupTenantId { get; set; }

        public string VendorTenantId { get; set; }

        public string SubContractorTenantId { get; set; }

        public string ClientTenantId { get; set; }

        public string SubClientTenantId { get; set; }

        public string WorkOrderId { get; set; }

        public string SourceSystemWorkOrderNumber { get; set; }

        public string SourceSystemOrderNumber { get; set; }
    }
}
