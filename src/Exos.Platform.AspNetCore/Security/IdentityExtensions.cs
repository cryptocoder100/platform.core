#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1307 // Specify StringComparison

namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Principal;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Identity Extensions.
    /// </summary>
    public static class IdentityExtensions
    {
        /// <summary>
        /// The claim used by <see cref="GetServicerTenantId(IIdentity)" />.
        /// </summary>
        public const string ServicerTenantIdClaim = "servicertenantidentifier";

        /// <summary>
        /// The claim used by <see cref="GetWorkorderServicerGroupTenantId(IIdentity)" />.
        /// </summary>
        public const string WorkorderServicerGroupTenantIdClaim = "woservicergrouptenantid";

        /// <summary>
        /// The claim used by <see cref="GetWorkOrderVendorTenantId(IIdentity)" />.
        /// </summary>
        public const string WorkOrderVendorTenantIdClaim = "wovendortenantid";

        /// <summary>
        /// The claim used by <see cref="GetWorkOrderSubcontractorTenantId(IIdentity)" />.
        /// </summary>
        public const string WorkOrderSubcontractorTenantIdClaim = "wosubcontractortenantid";

        /// <summary>
        /// The claim used by <see cref="GetWorkOrderMasterTenantId(IIdentity)" />.
        /// </summary>
        public const string WorkOrderMasterTenantIdClaim = "woclienttenantid";

        /// <summary>
        /// The claim used by <see cref="GetWorkOrderSubClientTenantId(IIdentity)" />.
        /// </summary>
        public const string WorkOrderSubClientTenantIdClaim = "wosubclienttenantid";

        /// <summary>
        /// The claim used by <see cref="GetSourceSystemWorkOrderNumber(IIdentity)" />.
        /// </summary>
        public const string SourceSystemWorkOrderNumberClaim = "sourcesystemworkordernumber";

        /// <summary>
        /// The claim used by <see cref="GetSourceSystemOrderNumber(IIdentity)" />.
        /// </summary>
        public const string SourceSystemOrderNumberClaim = "sourcesystemordernumber";

        /// <summary>
        /// The claim used by <see cref="GetHeaderWorkOrderId(IIdentity)" />.
        /// </summary>
        public const string HeaderWorkOrderIdClaim = "headerworkorderid";

        /// <summary>
        /// Returns the identifier of the current identity.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>ClaimTypes.NameIdentifier value.</returns>
        public static string GetUserId(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null)
                {
                    return claim.Value;
                }
            }

            return default(string);
        }

        /// <summary>
        /// Returns the string representation of the tenant type for the current identity.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims tenanttype value.</returns>
        public static string GetTenantType(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst("tenanttype");
                if (claim != null)
                {
                    return claim.Value;
                }
            }

            return default(string);
        }

        /// <summary>
        /// Returns the tenant identifier for the current identity.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims tenantidentifier value.</returns>
        public static long GetTenantId(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst("tenantidentifier");
                if (claim != null && long.TryParse(claim.Value, out long tenantId))
                {
                    return tenantId;
                }
            }

            return default(int);
        }

        /// <summary>
        /// Returns a list of "line of business" identifiers for the current user.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims lob.</returns>
        public static List<long> GetLinesOfBusiness(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var lobs = new List<long>();
                foreach (var claim in ident.FindAll("lob"))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        lobs.Add(id);
                    }
                }

                if (lobs.Count > 0)
                {
                    return lobs;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of Subcontractor identifiers when the current identity is a Vendor.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims subcontractor.</returns>
        public static List<long> GetSubcontractors(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var subcontractors = new List<long>();
                foreach (var claim in ident.FindAll("subcontractor"))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        subcontractors.Add(id);
                    }
                }

                if (subcontractors.Count > 0)
                {
                    return subcontractors;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of Sub Client identifiers when the current identity is a Master Client.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims subclient.</returns>
        public static List<long> GetSubClients(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var subclients = new List<long>();
                foreach (var claim in ident.FindAll("subclient"))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        subclients.Add(id);
                    }
                }

                if (subclients.Count > 0)
                {
                    return subclients;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of Master Client identifiers when the current identity is a Sub Client.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims masterclient.</returns>
        public static List<long> GetMasterClients(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var masterclients = new List<long>();
                foreach (var claim in ident.FindAll("masterclient"))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        masterclients.Add(id);
                    }
                }

                if (masterclients.Count > 0)
                {
                    return masterclients;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of Vendor identifiers when the current identity is a Subcontractor.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims vendor.</returns>
        public static List<long> GetVendors(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var vendors = new List<long>();
                foreach (var claim in ident.FindAll("vendor"))
                {
                    long id;
                    if (long.TryParse(claim.Value, out id))
                    {
                        vendors.Add(id);
                    }
                }

                if (vendors.Count > 0)
                {
                    return vendors;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of resources accessible by the current identity.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of Roles.</returns>
        public static List<string> GetRoles(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var roles = new List<string>(ident.FindAll(ClaimTypes.Role).Select(c => c.Value).Where(s => !string.IsNullOrEmpty(s)));
                return roles;
            }

            return default(List<string>);
        }

        /// <summary>
        /// Returns a list of resources accessible by the current identity.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of Resources.</returns>
        public static IEnumerable<string> GetResources(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var resourceValue = ident.FindFirst("resources");
                if (resourceValue != null)
                {
                    var resources = resourceValue.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    return resources;
                }
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Returns a list of Servicer Groups identifiers for the current user.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <param name="systemFunction">System Function.</param>
        /// <returns>List of values for the Claims servicergroup: + systemFunction.</returns>
        public static List<long> GetServicerGroups(this IIdentity identity, string systemFunction)
        {
            if (identity is ClaimsIdentity ident)
            {
                var ids = new List<long>();

                var name = (systemFunction ?? string.Empty).ToLowerInvariant().Replace(" ", string.Empty);

                foreach (var claim in ident.FindAll("servicergroup:" + name))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        ids.Add(id);
                    }
                }

                if (ids.Count > 0)
                {
                    return ids;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of Servicer Groups identifiers for the current user.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims servicergroup.</returns>
        public static List<long> GetServicerGroups(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var ids = new List<long>();
                foreach (var claim in ident.FindAll(cl => cl.Type.StartsWith("servicergroup:", StringComparison.OrdinalIgnoreCase)))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        ids.Add(id);
                    }
                }

                if (ids.Count > 0)
                {
                    return ids;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns a list of "AssociatedServicerTenantIds" identifiers for the current user.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims associatedservicertenantidentifier.</returns>
        public static List<long> GetAssociatedServicerTenantIds(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var lobs = new List<long>();
                foreach (var claim in ident.FindAll("associatedservicertenantidentifier"))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        lobs.Add(id);
                    }
                }

                ident = null;
                if (lobs.Count > 0)
                {
                    return lobs;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Returns Servicer Tenant Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims servicertenantidentifier value.</returns>
        public static long GetServicerTenantId(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(ServicerTenantIdClaim);
                if (claim != null && long.TryParse(claim.Value, out long servicerTenantId))
                {
                    ident = null;
                    return servicerTenantId;
                }
            }

            return default(int);
        }

        /// <summary>
        /// Returns Work Order Servicer Group Tenant Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims woservicergrouptenantid value.</returns>
        public static long GetWorkorderServicerGroupTenantId(this IIdentity identity)
        {
            long woservicergrouptenantidToParse = 0;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(WorkorderServicerGroupTenantIdClaim);
                if (claim != null && long.TryParse(claim.Value, out woservicergrouptenantidToParse))
                {
                    ident = null;
                    return woservicergrouptenantidToParse;
                }
            }

            return woservicergrouptenantidToParse;
        }

        /// <summary>
        /// Returns Work Order Vendor Tenant Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims wovendortenantid value.</returns>
        public static long GetWorkOrderVendorTenantId(this IIdentity identity)
        {
            long wovendortenantidToParse = 0;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(WorkOrderVendorTenantIdClaim);
                if (claim != null && long.TryParse(claim.Value, out wovendortenantidToParse))
                {
                    ident = null;
                    return wovendortenantidToParse;
                }
            }

            return wovendortenantidToParse;
        }

        /// <summary>
        /// Returns Work Order SubContractor Tenant Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims wosubcontractortenantid value.</returns>
        public static long GetWorkOrderSubcontractorTenantId(this IIdentity identity)
        {
            long wosubcontractortenantidToParse = 0;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(WorkOrderSubcontractorTenantIdClaim);
                if (claim != null && long.TryParse(claim.Value, out wosubcontractortenantidToParse))
                {
                    ident = null;
                    return wosubcontractortenantidToParse;
                }
            }

            return wosubcontractortenantidToParse;
        }

        /// <summary>
        /// Returns Work Order Client Tenant Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims woclienttenantid value.</returns>
        public static long GetWorkOrderMasterTenantId(this IIdentity identity)
        {
            long woclienttenantidToParse = 0;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(WorkOrderMasterTenantIdClaim);
                if (claim != null && long.TryParse(claim.Value, out woclienttenantidToParse))
                {
                    ident = null;
                    return woclienttenantidToParse;
                }
            }

            return woclienttenantidToParse;
        }

        /// <summary>
        /// Returns Work Order Sub Client Tenant Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims wosubclienttenantid value.</returns>
        public static long GetWorkOrderSubClientTenantId(this IIdentity identity)
        {
            long wosubclienttenantidToParse = 0;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(WorkOrderSubClientTenantIdClaim);
                if (claim != null && long.TryParse(claim.Value, out wosubclienttenantidToParse))
                {
                    ident = null;
                    return wosubclienttenantidToParse;
                }
            }

            return wosubclienttenantidToParse;
        }

        /// <summary>
        /// Returns Header Work Order Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims headerworkorderid value.</returns>
        public static long GetHeaderWorkOrderId(this IIdentity identity)
        {
            long headerworkorderidToParse = 0;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(HeaderWorkOrderIdClaim);
                if (claim != null && long.TryParse(claim.Value, out headerworkorderidToParse))
                {
                    ident = null;
                    return headerworkorderidToParse;
                }
            }

            return headerworkorderidToParse;
        }

        /// <summary>
        /// Returns Source System Work Order Number.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims sourcesystemworkordernumber value.</returns>
        public static string GetSourceSystemWorkOrderNumber(this IIdentity identity)
        {
            string sourceSystemWorkOrderNumberToParse = null;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(SourceSystemWorkOrderNumberClaim);
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                {
                    ident = null;
                    sourceSystemWorkOrderNumberToParse = claim.Value;
                    return sourceSystemWorkOrderNumberToParse;
                }
            }

            return sourceSystemWorkOrderNumberToParse;
        }

        /// <summary>
        /// Returns Source System Order Number.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims sourcesystemordernumber value.</returns>
        public static string GetSourceSystemOrderNumber(this IIdentity identity)
        {
            string sourceSystemOrderNumberToParse = null;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(SourceSystemOrderNumberClaim);
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                {
                    ident = null;
                    sourceSystemOrderNumberToParse = claim.Value;
                    return sourceSystemOrderNumberToParse;
                }
            }

            return sourceSystemOrderNumberToParse;
        }

        /// <summary>
        /// Returns a list of Servicer Features.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims servicerfeature.</returns>
        public static List<string> GetServicerTenantFeatures(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var servicerFeatures = new List<string>();
                foreach (var claim in ident.FindAll("servicerfeature"))
                {
                    servicerFeatures.Add(claim.Value);
                }

                ident = null;
                if (servicerFeatures.Count > 0)
                {
                    return servicerFeatures;
                }
            }

            return default(List<string>);
        }

        /// <summary>
        /// Get the Request Tracking Id.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor.</param>
        /// <returns>Request Tracking Id.</returns>
        public static string GetTrackingId(this IIdentity identity, IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor?.HttpContext?.GetTrackingId();
        }

        /// <summary>
        /// Get User teams.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims userteam.</returns>
        public static List<string> GetTeamsIds(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var ids = new List<string>();
                foreach (var claim in ident.FindAll(cl => cl.Type == "userteam"))
                {
                    if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    {
                        ids.Add(claim.Value);
                    }
                }

                ident = null;
                if (ids.Count > 0)
                {
                    return ids;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns if current identity is manager.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>True if found Claims ismanager, false otherwise.</returns>
        public static bool? GetIsManager(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst("ismanager");
                if (claim != null && bool.TryParse(claim.Value, out bool ismanager))
                {
                    return ismanager;
                }
            }

            return null;
        }

        /// <summary>
        /// This is the list of work orders when consumer is logged in.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims claimworkorderid.</returns>
        public static List<long> GetWorkOrderIds(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var ids = new List<long>();
                foreach (var claim in ident.FindAll(cl => cl.Type == "claimworkorderid"))
                {
                    if (long.TryParse(claim.Value, out long id))
                    {
                        ids.Add(id);
                    }
                }

                ident = null;
                if (ids.Count > 0)
                {
                    return ids;
                }
            }

            return default(List<long>);
        }

        /// <summary>
        /// Get Operational Types.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>List of values for the Claims operationaltype.</returns>
        public static List<int> GetOperationalTypes(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var ids = new List<int>();
                foreach (var claim in ident.FindAll(cl => cl.Type == "operationaltype"))
                {
                    if (int.TryParse(claim.Value, out int id))
                    {
                        ids.Add(id);
                    }
                }

                ident = null;
                if (ids.Count > 0)
                {
                    return ids;
                }
            }

            return default(List<int>);
        }

        /// <summary>
        /// Returns SubTenantType.
        /// </summary>
        /// <param name="identity">Current Identity.</param>
        /// <returns>Claims SubTenantType value.</returns>
        public static string GetSubTenantType(this IIdentity identity)
        {
            string subTenantType = null;
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst("subtenanttype");
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                {
                    ident = null;
                    subTenantType = claim.Value;
                    return subTenantType;
                }
            }

            return subTenantType;
        }

        /// <summary>
        /// Return the user First Name mapped to UserModel.FirstName.
        /// </summary>
        /// <param name="identity">The identity<see cref="IIdentity"/>.</param>
        /// <returns>The <see cref="string"/> for the First Name.</returns>
        public static string GetGivenName(this IIdentity identity)
        {
            if (identity is ClaimsIdentity ident)
            {
                var claim = ident.FindFirst(ClaimTypes.GivenName);
                if (claim != null)
                {
                    return claim.Value;
                }
            }

            return default;
        }

        /// <summary>
        /// Return the Value of the exosadmin claim, mapped to UserModel.ExosAdmin.
        /// </summary>
        /// <param name="identity">The identity<see cref="IIdentity"/>.</param>
        /// <returns>The <see cref="bool"/> for the exosadmin claim.</returns>
        public static bool? GetExosAdmin(this IIdentity identity)
        {
            if (identity is ClaimsIdentity claimsIdentity)
            {
                var claim = claimsIdentity.FindFirst(ExosClaimTypes.ExosAdmin);
                if (claim != null && bool.TryParse(claim.Value, out bool isExosAdmin))
                {
                    return isExosAdmin;
                }
            }

            return false;
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA1307 // Specify StringComparison