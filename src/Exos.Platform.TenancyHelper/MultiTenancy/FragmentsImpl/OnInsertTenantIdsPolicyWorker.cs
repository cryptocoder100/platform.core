#pragma warning disable CA1502 // Avoid excessive complexity
#pragma warning disable CA1822 // Mark members as static
namespace Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl
{
    using System;
    using System.Collections.Generic;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.Models;

    /// <summary>
    /// Apply MultiTenancy Policy Document on Insert.
    /// </summary>
    internal class OnInsertTenantIdsPolicyWorker : TenantParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnInsertTenantIdsPolicyWorker"/> class.
        /// </summary>
        /// <param name="policyContext">Policy Context.</param>
        public OnInsertTenantIdsPolicyWorker(IPolicyContext policyContext) : base(policyContext)
        {
            IsInsertPolicy = true;
        }

        /// <summary>
        /// Validate if MultiTenancy Insert Policy is valid.
        /// </summary>
        /// <param name="insertPolicyFragment">Insert Policy.</param>
        public void ValidateOnInsertTenantIdsPolicyFragment(dynamic insertPolicyFragment)
        {
            if (insertPolicyFragment == null)
            {
                throw new ArgumentNullException(nameof(insertPolicyFragment));
            }
        }

        /// <summary>
        /// Parse MultiTenancy Policy Document on Insert.
        /// </summary>
        /// <param name="insertPolicyFragment">Insert MultiTenancy Policy.</param>
        /// <param name="objectWithTenantIds">Object to apply Insert Policy.</param>
        /// <returns>Object with Insert Policy applied.</returns>
        public object DoWorkOnInsert(dynamic insertPolicyFragment, object objectWithTenantIds)
        {
            // Basic validation
            ValidateOnInsertTenantIdsPolicyFragment(insertPolicyFragment);

            // Set the values for SQL Server Entities
            if (typeof(Entities.ITenant).IsAssignableFrom(objectWithTenantIds.GetType()))
            {
                var objectWithTenantIdsBase = objectWithTenantIds as Entities.ITenant;
                if (objectWithTenantIdsBase != null)
                {
                    // Get and Set VendorTenantId
                    var vendorTenantId = GetTenantId(insertPolicyFragment.vendorTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.VendorTenantId = vendorTenantId != null && vendorTenantId.Count > 0 ? Convert.ToInt32(vendorTenantId[0]) : 0;

                    // Get and Set SubClientTenantId
                    var subClientTenantId = GetTenantId(insertPolicyFragment.subClientTenantId, objectWithTenantIdsBase);
                    objectWithTenantIdsBase.SubClientTenantId = subClientTenantId != null && subClientTenantId.Count > 0 ? Convert.ToInt32(subClientTenantId[0]) : 0;

                    // Get and Set ClientTenantId
                    var clientTenantId = GetTenantId(insertPolicyFragment.masterClientTenantId, objectWithTenantIdsBase);
                    objectWithTenantIdsBase.ClientTenantId = clientTenantId != null && clientTenantId.Count > 0 ? Convert.ToInt32(clientTenantId[0]) : (short)0;

                    // Get and Set SubcontractorTenantId
                    var subContractorTenantId = GetTenantId(insertPolicyFragment.subContractorTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.SubContractorTenantId = subContractorTenantId != null && subContractorTenantId.Count > 0 ? Convert.ToInt32(subContractorTenantId[0]) : 0;

                    // Get and Set ServicerGroupTenantId
                    var servicerGroupTenantId = GetTenantId(insertPolicyFragment.servicerGroupTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.ServicerGroupTenantId = servicerGroupTenantId != null && servicerGroupTenantId.Count > 0 ? Convert.ToInt16(servicerGroupTenantId[0]) : (short)0;

                    // Get and Set ServicerTenantId
                    var servicerTenantId = GetTenantId(insertPolicyFragment.servicerTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.ServicerTenantId = servicerTenantId != null && servicerTenantId.Count > 0 ? Convert.ToInt16(servicerTenantId[0]) : (short)0;

                    if (objectWithTenantIdsBase.VendorTenantId == objectWithTenantIdsBase.SubContractorTenantId && objectWithTenantIdsBase.SubContractorTenantId > 0)
                    {
                        objectWithTenantIdsBase.VendorTenantId = 0;
                    }
                }
            }
            else
            {
                var objectWithTenantIdsBase = objectWithTenantIds as BaseModel;
                if (objectWithTenantIdsBase != null)
                {
                    if (objectWithTenantIdsBase.Tenant == null)
                    {
                        objectWithTenantIdsBase.Tenant = new TenantModel();
                    }
                }

                if (objectWithTenantIdsBase != null)
                {
                    // Get and Set VendorTenantId
                    var vendorTenantId = GetTenantId(insertPolicyFragment.vendorTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.Vendor = vendorTenantId != null && vendorTenantId.Count > 0 ? vendorTenantId[0] : 0;

                    // Get and Set ClientTenantId
                    var subClient = GetTenantId(insertPolicyFragment.subClientTenantId, objectWithTenantIdsBase);
                    objectWithTenantIdsBase.Tenant.SubClient = subClient != null && subClient.Count > 0 ? subClient[0] : 0;

                    // Get and Set ClientTenantId
                    var masterClient = GetTenantId(insertPolicyFragment.masterClientTenantId, objectWithTenantIdsBase);
                    objectWithTenantIdsBase.Tenant.MasterClient = masterClient != null && masterClient.Count > 0 ? masterClient[0] : 0;

                    // Get and Set SubcontractorTenantId
                    var subContractor = GetTenantId(insertPolicyFragment.subContractorTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.SubContractor = subContractor != null && subContractor.Count > 0 ? subContractor[0] : 0;

                    // Get and Set ServicerGroupTenantId
                    var servicerGroup = GetTenantId(insertPolicyFragment.servicerGroupTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.ServicerGroup = servicerGroup != null && servicerGroup.Count > 0 ? servicerGroup[0] : 0;

                    // Get and Set ServicerTenantId
                    var servicerIds = GetTenantId(insertPolicyFragment.servicerTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.ServicerIds = servicerIds != null && servicerIds.Count > 0 ? servicerIds : new List<long>();

                    if (objectWithTenantIdsBase.Tenant.Vendor == objectWithTenantIdsBase.Tenant.SubContractor && objectWithTenantIdsBase.Tenant.SubContractor > 0)
                    {
                        objectWithTenantIdsBase.Tenant.Vendor = 0;
                    }
                }
            }

            // If validated, we will move further.
            return objectWithTenantIds;
        }
    }
}
#pragma warning restore CA1502 // Avoid excessive complexity
#pragma warning restore CA1822 // Mark members as static