#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter
#pragma warning disable CA1502 // Avoid excessive complexity
#pragma warning disable CA1822 // Mark members as static
namespace Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl
{
    using System;
    using System.Collections.Generic;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.Models;

    /// <summary>
    /// Parse MultiTenancy Policy Document on Update.
    /// </summary>
    internal class OnUpdateTenantIdsPolicyWorker : TenantParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnUpdateTenantIdsPolicyWorker"/> class.
        /// </summary>
        /// <param name="policyContext">Policy Context.</param>
        public OnUpdateTenantIdsPolicyWorker(IPolicyContext policyContext) : base(policyContext)
        {
            IsInsertPolicy = false;
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
        /// Parse MultiTenancy Policy Document on Update.
        /// </summary>
        /// <param name="updatePolicyFragment">Update MultiTenancy Policy.</param>
        /// <param name="objectWithTenantIds">Object to apply Update Policy.</param>
        /// <returns>Object with Update Policy applied.</returns>
        public object DoWorkOnUpdate(dynamic updatePolicyFragment, object objectWithTenantIds)
        {
            ValidateOnInsertTenantIdsPolicyFragment(updatePolicyFragment);

            // Set the values for SQL Server Entities
            if (typeof(Entities.ITenant).IsAssignableFrom(objectWithTenantIds.GetType()))
            {
                var objectWithTenantIdsBase = objectWithTenantIds as Entities.ITenant;
                var vendorTenantId = GetTenantId(updatePolicyFragment.vendorTenantId, objectWithTenantIds);
                if (vendorTenantId != null && vendorTenantId.Count > 0)
                {
                    // No update
                    if (vendorTenantId[0] != -4)
                    {
                        objectWithTenantIdsBase.VendorTenantId = Convert.ToInt32(vendorTenantId[0]);
                    }
                }

                // Get and Set SubClientTenantId
                var subClientTenantId = GetTenantId(updatePolicyFragment.subClientTenantId, objectWithTenantIdsBase);
                if (subClientTenantId != null && subClientTenantId.Count > 0)
                {
                    // No update
                    if (subClientTenantId[0] != -4)
                    {
                        objectWithTenantIdsBase.SubClientTenantId = Convert.ToInt32(subClientTenantId[0]);
                    }
                }

                // Get and Set ClientTenantId
                var clientTenantId = GetTenantId(updatePolicyFragment.masterClientTenantId, objectWithTenantIdsBase);
                if (clientTenantId != null && clientTenantId.Count > 0)
                {
                    // No update
                    if (clientTenantId[0] != -4)
                    {
                        objectWithTenantIdsBase.ClientTenantId = Convert.ToInt32(clientTenantId[0]);
                    }
                }

                // Get and Set SubcontractorTenantId
                var subContractorTenantId = GetTenantId(updatePolicyFragment.subContractorTenantId, objectWithTenantIds);
                if (subContractorTenantId != null && subContractorTenantId.Count > 0)
                {
                    // No update
                    if (subContractorTenantId[0] != -4)
                    {
                        objectWithTenantIdsBase.SubContractorTenantId = Convert.ToInt32(subContractorTenantId[0]);
                    }
                }

                // Get and Set ServicerGroupTenantId
                var servicerGroupTenantId = GetTenantId(updatePolicyFragment.servicerGroupTenantId, objectWithTenantIds);
                if (servicerGroupTenantId != null && servicerGroupTenantId.Count > 0)
                {
                    // No update
                    if (servicerGroupTenantId[0] != -4)
                    {
                        objectWithTenantIdsBase.ServicerGroupTenantId = Convert.ToInt16(servicerGroupTenantId[0]);
                    }
                }

                // Get and Set ServicerTenantId
                var servicerTenantId = GetTenantId(updatePolicyFragment.servicerTenantId, objectWithTenantIds);
                if (servicerTenantId != null && servicerTenantId.Count > 0)
                {
                    // No update
                    if (servicerTenantId[0] != -4)
                    {
                        objectWithTenantIdsBase.ServicerTenantId = Convert.ToInt16(servicerTenantId[0]);
                    }
                }

                if (objectWithTenantIdsBase.VendorTenantId == objectWithTenantIdsBase.SubContractorTenantId &&
                    objectWithTenantIdsBase.SubContractorTenantId > 0)
                {
                    objectWithTenantIdsBase.VendorTenantId = 0;
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
                    var vendorTenantId = GetTenantId(updatePolicyFragment.vendorTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.Vendor = vendorTenantId != null && vendorTenantId.Count > 0 ? vendorTenantId[0] : 0;

                    // Get and Set ClientTenantId
                    var subClient = GetTenantId(updatePolicyFragment.subClientTenantId, objectWithTenantIdsBase);
                    objectWithTenantIdsBase.Tenant.SubClient = subClient != null && subClient.Count > 0 ? subClient[0] : 0;

                    // Get and Set ClientTenantId
                    var masterClient = GetTenantId(updatePolicyFragment.masterClientTenantId, objectWithTenantIdsBase);
                    objectWithTenantIdsBase.Tenant.MasterClient = masterClient != null && masterClient.Count > 0 ? masterClient[0] : 0;

                    // Get and Set SubcontractorTenantId
                    var subContractor = GetTenantId(updatePolicyFragment.subContractorTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.SubContractor = subContractor != null && subContractor.Count > 0 ? subContractor[0] : 0;

                    // Get and Set ServicerGroupTenantId
                    var servicerGroup = GetTenantId(updatePolicyFragment.servicerGroupTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.ServicerGroup = servicerGroup != null && servicerGroup.Count > 0 ? servicerGroup[0] : 0;

                    // Get and Set ServicerTenantId
                    var servicerIds = GetTenantId(updatePolicyFragment.servicerTenantId, objectWithTenantIds);
                    objectWithTenantIdsBase.Tenant.ServicerIds = servicerIds != null && servicerIds.Count > 0 ? servicerIds : new List<long>();

                    if (objectWithTenantIdsBase.Tenant.Vendor == objectWithTenantIdsBase.Tenant.SubContractor && objectWithTenantIdsBase.Tenant.SubContractor > 0)
                    {
                        // Test
                        objectWithTenantIdsBase.Tenant.Vendor = 0;
                    }
                }
            }

            // If validated, we will move further.
            return objectWithTenantIds;
        }
    }
}
#pragma warning restore SA1300 // ElementMustBeginWithUpperCaseLetter
#pragma warning restore CA1502 // Avoid excessive complexity
#pragma warning restore CA1822 // Mark members as static