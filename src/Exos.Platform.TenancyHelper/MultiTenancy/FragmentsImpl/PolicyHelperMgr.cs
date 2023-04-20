#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1502 // Avoid excessive complexity

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.TenancyHelper.Interfaces;
using Exos.Platform.TenancyHelper.PersistenceService;
using Exos.Platform.TenancyHelper.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.FormattableString;

namespace Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl;

/// <summary>
/// Helper Class to Process MultiTenancy policy.
/// </summary>
public class PolicyHelperMgr
{
    /// <summary>
    /// MultiTenancy policy files must match this suffix.
    /// </summary>
    public const string PolicyDocumentSuffix = "PolicyDocument";

    private const int FullAccess = 777;

    private readonly List<string> _needsRelationshipQuery = new List<string>()
        {
            "VendorProfile",
            "SubContractorProfile",
            "MasterClientProfile",
            "SubClientProfile",
            "VendorAttribute",
            "SubContractorAttribute",
            "MasterClientAttribute",
            "User",
            "VendorNote",
            "ClientNote",
            "SubContractorAdditionalInfo",
            "VendorAdditionalInfo",
        };

    private readonly IDocumentClientAccessor _documentClientAccessor;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger _logger;
    private readonly IUserContextService _userContextService;
    private readonly PlatformDefaultsOptions _platformDefaultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyHelperMgr"/> class.
    /// </summary>
    /// <param name="documentClientAccessor">The document client accessor.</param>
    /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="userCtxService">The user ctx service.</param>
    /// <param name="platformDefaultsOptions">The platform defaults options.</param>
    public PolicyHelperMgr(IDocumentClientAccessor documentClientAccessor, IMemoryCache memoryCache, ILogger logger, IUserContextService userCtxService, PlatformDefaultsOptions platformDefaultsOptions)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);

        _documentClientAccessor = documentClientAccessor;
        _memoryCache = memoryCache;
        _logger = logger;
        _userContextService = userCtxService;
        _platformDefaultOptions = platformDefaultsOptions;
    }

    /// <summary>
    /// Get Policy Object.
    /// </summary>
    /// <param name="objectWithTenantIds">Object to apply the Tenancy Policy.</param>
    /// <returns>Policy Object.</returns>
    public Task<object> GetPolicyObject(object objectWithTenantIds)
    {
        if (objectWithTenantIds == null)
        {
            return null;
        }

        var policyName = GetPolicyDocumentName(objectWithTenantIds);
        if (string.IsNullOrEmpty(policyName))
        {
            return null;
        }

        return GetPolicyObjectByName(policyName);
    }

    /// <summary>
    /// Apply MultiTenancy Policy Document on Insert.
    /// </summary>
    /// <param name="objectWithTenantIds">Object to apply Multi-Tenant policy.</param>
    /// <param name="policyContext">Policy Context.</param>
    /// <returns>Object with Multi-Tenant policy applied.</returns>
    public async Task<object> HandleInsert(object objectWithTenantIds, IPolicyContext policyContext)
    {
        if (objectWithTenantIds != null)
        {
            object policyDocJObject = null;
            dynamic policyDocDynamic = null;

            if (policyContext == null || (string.IsNullOrEmpty(policyContext.PolicyDoc) && string.IsNullOrEmpty(policyContext.PolicyDocName)))
            {
                var policy = await GetPolicyObject(objectWithTenantIds).ConfigureAwait(false);
                if (policy != null)
                {
                    policyDocJObject = policy;
                    policyDocDynamic = policyDocJObject;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(policyContext.PolicyDoc))
                {
                    policyDocJObject = JObject.Parse(policyContext.PolicyDoc);
                    policyDocDynamic = policyDocJObject;
                }
                else
                {
                    if (!string.IsNullOrEmpty(policyContext.PolicyDocName))
                    {
                        var policy = await GetPolicyObjectByName(policyContext.PolicyDocName).ConfigureAwait(false);
                        if (policy != null)
                        {
                            policyDocJObject = policy;
                            policyDocDynamic = policyDocJObject;
                        }
                    }
                }
            }

            if (policyDocJObject == null)
            {
                return objectWithTenantIds;
            }

            if (IsEntityMultiTenant(policyDocDynamic))
            {
                return new OnInsertTenantIdsPolicyWorker(policyContext).DoWorkOnInsert(policyDocDynamic.onInsertTenantIdsPolicy, objectWithTenantIds);
            }
            else
            {
                _logger.LogDebug("Insert, Tenant policy not applied as Subject to Multi-Tenancy is false Or not set.");
                return objectWithTenantIds;
            }
        }

        return objectWithTenantIds;
    }

    /// <summary>
    /// Apply MultiTenancy Policy Document on Update.
    /// </summary>
    /// <param name="objectWithTenantIds">Object to apply Multi-Tenant policy.</param>
    /// <param name="policyContext">Policy Context.</param>
    /// <returns>Object with Multi-Tenant policy applied.</returns>
    public async Task<object> HandleUpdate(object objectWithTenantIds, IPolicyContext policyContext)
    {
        object policyDocJObject = null;
        dynamic policyDocDynamic = null;

        // no policy specified.
        if (policyContext == null || (string.IsNullOrEmpty(policyContext.PolicyDoc) && string.IsNullOrEmpty(policyContext.PolicyDocName)))
        {
            var policy = await GetPolicyObject(objectWithTenantIds).ConfigureAwait(false);
            if (policy != null)
            {
                policyDocJObject = policy;
                policyDocDynamic = policyDocJObject;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(policyContext.PolicyDoc))
            {
                policyDocJObject = JObject.Parse(policyContext.PolicyDoc);
                policyDocDynamic = policyDocJObject;
            }
            else
            {
                if (!string.IsNullOrEmpty(policyContext.PolicyDocName))
                {
                    var policy = await GetPolicyObjectByName(policyContext.PolicyDocName).ConfigureAwait(false);
                    if (policy != null)
                    {
                        policyDocJObject = policy;
                        policyDocDynamic = policyDocJObject;
                    }
                }
            }
        }

        if (policyDocJObject == null)
        {
            return objectWithTenantIds;
        }

        if (IsEntityMultiTenant(policyDocDynamic) && IsTenantIdsUpdatable(policyDocDynamic))
        {
            return new OnUpdateTenantIdsPolicyWorker(policyContext).DoWorkOnUpdate(policyDocDynamic.onUpdateTenantIdsPolicy, objectWithTenantIds);
        }
        else
        {
            _logger.LogDebug("Update, Tenant policy not applied as Subject to Multi-Tenancy is false Or not set OR isUpdateToTenantIdsPermitted is set to false.");
            return objectWithTenantIds;
        }
    }

    /// <summary>
    /// Validate if TenantID can be updated.
    /// </summary>
    /// <param name="policyDocDynamic">Policy Document.</param>
    /// <returns>If TenantId can be updated.</returns>
    public bool IsTenantIdsUpdatable(dynamic policyDocDynamic)
    {
        if (policyDocDynamic == null)
        {
            return false;
        }

        return policyDocDynamic.isUpdateToTenantIdsPermitted != null && policyDocDynamic.isUpdateToTenantIdsPermitted == true;
    }

    /// <summary>
    /// Validate if Entity is available for Multi-Tenancy.
    /// </summary>
    /// <param name="policyDocDynamic">Policy Document.</param>
    /// <returns>If Multi-Tenancy condition applies to the entity.</returns>
    public bool IsEntityMultiTenant(dynamic policyDocDynamic)
    {
        if (policyDocDynamic == null)
        {
            return false;
        }

        return policyDocDynamic.isSubjectToMultiTenancy != null && policyDocDynamic.isSubjectToMultiTenancy == true;
    }

    /// <summary>
    /// Validate if Entity is available for Multi-Tenancy.
    /// </summary>
    /// <param name="policyContext">Policy Context.</param>
    /// <returns>If Multi-Tenancy condition applies to the entity.</returns>
    public async Task<bool> IsEntityMultiTenant(IPolicyContext policyContext)
    {
        if (policyContext == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(policyContext.PolicyDocName))
        {
            var policy = await GetPolicyObjectByName(policyContext.PolicyDocName).ConfigureAwait(false);
            if (policy != null)
            {
                object policyDocJObject = policy;
                dynamic policyDocDynamic = policyDocJObject;
                return IsEntityMultiTenant(policyDocDynamic);
            }
        }

        return false;
    }

    /// <summary>
    /// Read Policy Attributes.
    /// </summary>
    /// <param name="policyContext">Policy Context.</param>
    /// <returns>EntityPolicyAttributes.</returns>
    public async Task<EntityPolicyAttributes> ReadEntityPolicyAttributes(IPolicyContext policyContext)
    {
        // Setting this to true by default.
        var entityPolicyAttributes = new EntityPolicyAttributes()
        {
            IsCacheable = false,
            IsEntityMultiTenant = true,
            ApplyServicerFilterForVendorTenant = true,
        };

        if (policyContext == null)
        {
            return entityPolicyAttributes;
        }

        if (!string.IsNullOrEmpty(policyContext.PolicyDocName))
        {
            var policy = await GetPolicyObjectByName(policyContext.PolicyDocName).ConfigureAwait(false);
            if (policy != null)
            {
                object policyDocJObject = policy;
                dynamic policyDocDynamic = policyDocJObject;

                entityPolicyAttributes.IsEntityMultiTenant = IsEntityMultiTenant(policyDocDynamic);
                bool isCacheable = policyDocDynamic.isCacheable != null ? policyDocDynamic.isCacheable : false;
                bool applyServicerFilterForVendorTenant = policyDocDynamic.applyServicerFilterForVendorTenant != null ? policyDocDynamic.applyServicerFilterForVendorTenant : false;

                entityPolicyAttributes.IsCacheable = isCacheable;
                entityPolicyAttributes.ApplyServicerFilterForVendorTenant = applyServicerFilterForVendorTenant;
            }
        }

        return entityPolicyAttributes;
    }

    /// <summary>
    /// Get Cosmos Document Type.
    /// </summary>
    /// <param name="parameters">SqlParameterCollection.</param>
    /// <returns>Value on @cosmosDocType field.</returns>
    public string GetDocType(SqlParameterCollection parameters)
    {
        if (parameters != null && parameters.Where(p => p.Name == Constants.CosmosDocType).FirstOrDefault() != null)
        {
            var docType = parameters.Where(p => p.Name == Constants.CosmosDocType).FirstOrDefault();
            if (docType != null && docType.Value != null)
            {
                if (docType.Value.GetType() == typeof(string))
                {
                    return docType.Value.ToString();
                }
                else
                {
                    if (docType.Value.GetType() == typeof(List<string>))
                    {
                        var lst = docType.Value as List<string>;
                        if (lst != null && lst.Count > 0 && !string.IsNullOrEmpty(lst[0]))
                        {
                            return lst[0].ToString(CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Get Multi-Tenancy where condition for Cosmos DB query.
    /// </summary>
    /// <param name="tableOrAliasName">Table Name.</param>
    /// <param name="parameters">SqlParameterCollection.</param>
    /// <param name="entityPolicyAttributes">EntityPolicyAttributes.</param>
    /// <returns>Where clause with Multi-Tenancy condition.</returns>
    public string GetCosmosWhereClause(string tableOrAliasName, SqlParameterCollection parameters, EntityPolicyAttributes entityPolicyAttributes)
    {
        if (string.IsNullOrEmpty(tableOrAliasName))
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(entityPolicyAttributes);

        // Only run if user is authenticated
        if (string.IsNullOrEmpty(_userContextService?.Username) || string.IsNullOrEmpty(_userContextService?.TenantType))
        {
            return string.Empty;
        }

        // NOTE: As we build the query below we often inline data directly rather than parameterize it, because:
        //   a). This data comes from a trusted source such as user context
        //   b). The v2 SDK has a known issue with parameters within IN clauses: https://github.com/Azure/azure-cosmosdb-node/issues/156

        StringBuilder sb = new StringBuilder();

        var lobIds = new List<long> { -1 };
        lobIds.AddRange(_userContextService.LinesOfBusiness?.OrderBy(i => i) ?? Enumerable.Empty<long>());
        sb.AppendLine(Invariant($"  AND EXISTS (SELECT VALUE lobId FROM lobId IN v.tenant.lineofBusinessid WHERE lobId IN ({string.Join(", ", lobIds)}))"));

        switch (_userContextService.TenantType.ToLowerInvariant())
        {
            case "masterclient":
                {
                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" ( v.tenant.masterClient IN (-1,-2) AND v.tenant.subClient = 0 ) "); // this is to filter out subContractor records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (v.tenant.masterClient IN (-1,-2)) "); // for reference data kind of data.
                    }

                    // restrictedclient client user can work across multiple sub clients and master clients.
                    // restrictedmasterclient can only work under one masterclient but he has restricted to only couple of subclients under that branch
                    if (string.Equals(_userContextService.SubTenantType, "restrictedclient", StringComparison.OrdinalIgnoreCase) || string.Equals(_userContextService.SubTenantType, "restrictedmasterclient", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_userContextService.SubClientIds != null && _userContextService.SubClientIds.Count > 0 && _userContextService.MasterClientIds != null && _userContextService.MasterClientIds.Count > 0)
                        {
                            sb.Append(" OR ( v.tenant.masterClient IN ( " + string.Join(", ", _userContextService.MasterClientIds.ToArray()) + " ) "
                                    + " AND  v.tenant.subClient IN ( -1, " + string.Join(", ", _userContextService.SubClientIds.ToArray()) + " ) ) ");

                            sb.Append(" OR ( v.tenant.masterClient  IN (-1,-2) "
                                + " AND  v.tenant.subClient IN (" + string.Join(", ", _userContextService.SubClientIds.ToArray()) + " ) ) ");
                        }
                        else
                        {
                            sb.Append(" OR ( v.tenant.masterClient = " + _userContextService.TenantId + " ) ");
                        }
                    }
                    else
                    {
                        if (_userContextService.SubClientIds != null && _userContextService.SubClientIds.Count > 0)
                        {
                            sb.Append(" OR ( v.tenant.masterClient = " + _userContextService.TenantId
                                + " AND  v.tenant.subClient IN ( "
                                + string.Join(", ", _userContextService.SubClientIds.ToArray()) + " ) ) ");
                            sb.Append(" OR ( v.tenant.masterClient  IN (-1,-2) "
                                + " AND  v.tenant.subClient IN ("
                                + string.Join(", ", _userContextService.SubClientIds.ToArray()) + " ) ) ");
                        }

                        sb.Append(" OR ( v.tenant.masterClient =  " + _userContextService.TenantId + " ) ");
                    }

                    sb.AppendLine(" ) ");
                    break;
                }

            case "subclient":
                {
                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (  v.tenant.subClient IN (-1,-2) AND v.tenant.masterClient = 0 ) "); // this is to filter out subContractor records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (  v.tenant.subClient IN (-1,-2) ) "); // for reference data kind of data.
                    }

                    if (_userContextService.MasterClientIds != null && _userContextService.MasterClientIds.Count > 0)
                    {
                        sb.Append(" OR ( v.tenant.subClient = "
                            + _userContextService.TenantId
                            + " AND  v.tenant.masterClient IN ( "
                            + string.Join(", ", _userContextService.MasterClientIds.ToArray())
                            + " ) ) ");
                        sb.Append(" OR ( v.tenant.subClient  IN (-1,-2) "
                            + " AND  v.tenant.masterClient IN ( "
                            + string.Join(", ", _userContextService.MasterClientIds.ToArray())
                            + " ) ) ");
                    }

                    sb.AppendLine(" OR  ( v.tenant.subClient =  " + _userContextService.TenantId + " ) ");
                    sb.AppendLine("  ) ");
                    break;
                }

            case "vendor":
                {
                    if (entityPolicyAttributes.ApplyServicerFilterForVendorTenant && _userContextService.ServicerTenantId > 0)
                    {
                        if (_userContextService.ServicerTenantId > 0)
                        {
                            sb.AppendLine(" AND ( ");

                            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.ServicerTenantId))
                            {
                                // Previous implementation to include -1 to servicelink tenant
                                sb.Append(" ( ARRAY_CONTAINS(v.tenant.servicerIds, -1 ))");
                                sb.Append(" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, -2 ))");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-1,-2,{FullAccess})) ")); // remove this.
                            }
                            else
                            {
                                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                                sb.Append(" ( ARRAY_CONTAINS(v.tenant.servicerIds, -2 ))");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-2,{FullAccess})) "));
                            }

                            sb.Append(" OR  ( v.tenant.servicer= "
                                + _userContextService.ServicerTenantId.ToInvariantString()
                                + "  ) "); // remove this.
                            sb.Append(" OR  ( ARRAY_CONTAINS(v.tenant.servicerIds, "
                                + _userContextService.ServicerTenantId
                                + " ) ) ");
                            sb.AppendLine(" ) ");
                        }
                    }

                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (v.tenant.vendor IN (-1,-2) AND v.tenant.subContractor = 0) "); // this is to filter out subContractor records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (v.tenant.vendor IN (-1,-2)) "); // for reference data kind of data.
                    }

                    if (_userContextService.SubVendorIds != null && _userContextService.SubVendorIds.Count > 0)
                    {
                        sb.Append(" OR ( v.tenant.vendor =  "
                            + _userContextService.TenantId
                            + " AND v.tenant.subContractor IN ( "
                            + string.Join(", ", _userContextService.SubVendorIds.ToArray())
                            + ") )");
                        sb.Append(" OR ( v.tenant.vendor  IN (-1,-2) "
                            + " AND v.tenant.subContractor IN ( "
                            + string.Join(", ", _userContextService.SubVendorIds.ToArray())
                            + ") )");
                    }

                    sb.Append(" OR (v.tenant.vendor =  " + _userContextService.TenantId + ") ");
                    sb.AppendLine(" ) ");
                    break;
                }

            case "subcontractor":
                {
                    if (entityPolicyAttributes.ApplyServicerFilterForVendorTenant && _userContextService.ServicerTenantId > 0)
                    {
                        if (_userContextService.ServicerTenantId > 0)
                        {
                            sb.AppendLine(" AND ( ");
                            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.ServicerTenantId))
                            {
                                // Previous implementation to include -1 to servicelink tenant
                                sb.Append(" ( ARRAY_CONTAINS(v.tenant.servicerIds, -1  ) )");
                                sb.Append(" OR  ( ARRAY_CONTAINS(v.tenant.servicerIds, -2  ) )");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-1,-2,{FullAccess})) ")); // remove this.
                            }
                            else
                            {
                                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                                sb.Append(" ( ARRAY_CONTAINS(v.tenant.servicerIds, -2  ) )");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-2,{FullAccess})) "));
                            }

                            sb.Append(" OR  ( v.tenant.servicer= "
                                + _userContextService.ServicerTenantId.ToString(CultureInfo.InvariantCulture)
                                + "  ) "); // remove this.
                            sb.Append(" OR  ( ARRAY_CONTAINS(v.tenant.servicerIds, " + _userContextService.ServicerTenantId + " ) ) ");
                            sb.AppendLine(" ) ");
                        }
                    }

                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (v.tenant.subContractor IN (-1,-2) AND v.tenant.vendor = 0) "); // this is to filter out firms records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (v.tenant.subContractor IN (-1,-2)) "); // for reference data kind of data.
                    }

                    if (_userContextService.VendorIds != null && _userContextService.VendorIds.Count > 0)
                    {
                        sb.Append(" OR ( v.tenant.subContractor = "
                            + _userContextService.TenantId
                            + " AND v.tenant.vendor IN ("
                            + string.Join(", ", _userContextService.VendorIds.ToArray())
                            + " ) ) ");
                        sb.Append(" OR ( v.tenant.subContractor  IN (-1,-2)  "
                            + " AND v.tenant.vendor IN ( "
                            + string.Join(", ", _userContextService.VendorIds.ToArray())
                            + " ) ) ");
                    }

                    sb.Append(" OR ( v.tenant.subContractor =  " + _userContextService.TenantId + "  ) ");
                    sb.AppendLine("  ) ");
                    break;
                }

            case "servicer":
                {
                    var servicerTenantIds = new List<long>
                    {
                        _userContextService.TenantId
                    };

                    if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
                    {
                        // Previous implementation to include -1 to servicelink tenant
                        servicerTenantIds.Add(-1);
                        servicerTenantIds.Add(-2);
                        servicerTenantIds.Add(FullAccess);
                    }
                    else
                    {
                        // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                        servicerTenantIds.Add(-2);
                        servicerTenantIds.Add(FullAccess);
                    }

                    var servicerIds = string.Join(", ", servicerTenantIds);

                    sb.AppendLine(Invariant($"  AND (v.tenant.servicer IN ({servicerIds}) OR EXISTS (SELECT VALUE servicerId FROM servicerId IN v.tenant.servicerIds WHERE servicerId IN ({servicerIds})))"));
                    break;
                }

            default:
                {
                    throw new ArgumentException("Invalid user type");
                }
        }

        string query = sb.Replace("v.", tableOrAliasName).ToString();
        return query;
    }

    /// <summary>
    /// Get Multi-Tenancy where condition for Cosmos DB search query.
    /// </summary>
    /// <param name="tableOrAliasName">Table Name.</param>
    /// <param name="parameters">SqlParameterCollection.</param>
    /// <param name="entityPolicyAttributes">EntityPolicyAttributes.</param>
    /// <returns>Where clause with Multi-Tenancy condition.</returns>
    public string GetCosmosWhereClauseForSearches(string tableOrAliasName, SqlParameterCollection parameters, EntityPolicyAttributes entityPolicyAttributes)
    {
        if (string.IsNullOrEmpty(tableOrAliasName))
        {
            return string.Empty;
        }

        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(entityPolicyAttributes);

        // Only run if user is authenticated
        if (string.IsNullOrEmpty(_userContextService?.Username) || string.IsNullOrEmpty(_userContextService?.TenantType))
        {
            return string.Empty;
        }

        // NOTE: As we build the query below we often inline data directly rather than parameterize it, because:
        //   a). This data comes from a trusted source such as user context
        //   b). The v2 SDK has a known issue with parameters within IN clauses: https://github.com/Azure/azure-cosmosdb-node/issues/156

        StringBuilder sb = new StringBuilder();

        var lobIds = new List<long> { -1 };
        lobIds.AddRange(_userContextService.LinesOfBusiness?.OrderBy(i => i) ?? Enumerable.Empty<long>());
        sb.AppendLine(Invariant($"  AND EXISTS (SELECT VALUE lobId FROM lobId IN v.tenant.lineofBusinessid WHERE lobId IN ({string.Join(", ", lobIds)}))"));

        switch (_userContextService.TenantType.ToLowerInvariant())
        {
            case "masterclient":
                {
                    sb.AppendLine(" AND ( ");

                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (  v.tenant.masterClient IN (-1,-2) AND v.tenant.subClient = 0 )  "); // this is to filter out subContractor records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (  v.tenant.masterClient IN (-1,-2) ) "); // for reference data kind of data.
                    }

                    // restrictedclient client user can work across multiple sub clients and master clients.
                    if (string.Equals(_userContextService.SubTenantType, "restrictedclient", StringComparison.OrdinalIgnoreCase) || string.Equals(_userContextService.SubTenantType, "restrictedmasterclient", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_userContextService.SubClientIds != null && _userContextService.SubClientIds.Count > 0 && _userContextService.MasterClientIds != null && _userContextService.MasterClientIds.Count > 0)
                        {
                            sb.Append(" OR ( v.tenant.masterClient IN ( " + string.Join(", ", _userContextService.MasterClientIds.ToArray()) + " ) "
                                    + " AND  v.tenant.subClient IN ( -1, " + string.Join(", ", _userContextService.SubClientIds.ToArray()) + " ) ) ");

                            sb.Append(" OR ( v.tenant.masterClient  IN (-1,-2) "
                                + " AND  v.tenant.subClient IN (" + string.Join(", ", _userContextService.SubClientIds.ToArray()) + " ) ) ");
                        }
                        else
                        {
                            sb.Append(" OR ( v.tenant.masterClient =  " + _userContextService.TenantId + " ) ");
                        }
                    }
                    else
                    {
                        if (_userContextService.SubClientIds != null && _userContextService.SubClientIds.Count > 0)
                        {
                            sb.Append(" OR ( v.tenant.masterClient = "
                                + _userContextService.TenantId
                                + " AND  v.tenant.subClient IN ( "
                                + string.Join(", ", _userContextService.SubClientIds.ToArray())
                                + " ) ) ");
                            sb.Append(" OR ( v.tenant.masterClient  IN (-1,-2) "
                                + " AND  v.tenant.subClient IN ("
                                + string.Join(", ", _userContextService.SubClientIds.ToArray())
                                + " ) ) ");
                        }

                        sb.Append(" OR ( v.tenant.masterClient =  " + _userContextService.TenantId + " ) ");
                    }

                    sb.AppendLine("  ) ");
                    break;
                }

            case "subclient":
                {
                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (  v.tenant.subClient IN (-1,-2) AND v.tenant.masterClient = 0 )  "); // this is to filter out subContractor records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (  v.tenant.subClient IN (-1,-2) ) "); // for reference data kind of data.
                    }

                    if (_userContextService.MasterClientIds != null && _userContextService.MasterClientIds.Count > 0)
                    {
                        sb.Append(" OR ( v.tenant.subClient = "
                            + _userContextService.TenantId
                            + " AND  v.tenant.masterClient IN ( "
                            + string.Join(", ", _userContextService.MasterClientIds.ToArray())
                            + " ) ) ");
                        sb.Append(" OR ( v.tenant.subClient  IN (-1,-2) "
                            + " AND  v.tenant.masterClient IN ( "
                            + string.Join(", ", _userContextService.MasterClientIds.ToArray())
                            + " ) ) ");
                    }

                    sb.AppendLine(" OR  ( v.tenant.subClient =  " + _userContextService.TenantId + " ) ");
                    sb.AppendLine("  ) ");
                    break;
                }

            case "vendor":
                {
                    if (entityPolicyAttributes.ApplyServicerFilterForVendorTenant && _userContextService.ServicerTenantId > 0)
                    {
                        if (_userContextService.ServicerTenantId > 0)
                        {
                            sb.AppendLine(" AND ( ");

                            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.ServicerTenantId))
                            {
                                // Previous implementation to include -1 to servicelink tenant
                                sb.Append(" ( ARRAY_CONTAINS(v.tenant.servicerIds, -1  ) )");
                                sb.Append(" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, -2  ) )");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-1,-2,{FullAccess})) ")); // remove this.
                            }
                            else
                            {
                                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                                sb.Append(" ( ARRAY_CONTAINS(v.tenant.servicerIds, -2 ) )");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-2,{FullAccess})) "));
                            }

                            sb.Append(" OR  ( v.tenant.servicer= " + _userContextService.ServicerTenantId.ToInvariantString() + "  ) ");
                            sb.Append(" OR  ( ARRAY_CONTAINS(v.tenant.servicerIds, " + _userContextService.ServicerTenantId + " ) ) ");

                            sb.AppendLine("  ) ");
                        }
                    }

                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (  v.tenant.vendor IN (-1,-2) AND v.tenant.subContractor = 0 )  "); // this is to filter out subContractor records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (  v.tenant.vendor IN (-1,-2) ) "); // for reference data kind of data.
                    }

                    if (_userContextService.SubVendorIds != null && _userContextService.SubVendorIds.Count > 0)
                    {
                        sb.Append(" OR ( v.tenant.vendor =  "
                            + _userContextService.TenantId
                            + " AND v.tenant.subContractor IN ( "
                            + string.Join(", ", _userContextService.SubVendorIds.ToArray())
                            + ") )");
                        sb.Append(" OR ( v.tenant.vendor  IN (-1,-2) "
                            + " AND v.tenant.subContractor IN ( "
                            + string.Join(", ", _userContextService.SubVendorIds.ToArray())
                            + ") )");
                    }

                    sb.Append(" OR ( v.tenant.vendor =  " + _userContextService.TenantId + "  ) ");
                    sb.AppendLine("  ) ");
                    break;
                }

            case "subcontractor":
                {
                    if (entityPolicyAttributes.ApplyServicerFilterForVendorTenant && _userContextService.ServicerTenantId > 0)
                    {
                        if (_userContextService.ServicerTenantId > 0)
                        {
                            sb.AppendLine(" AND ( ");

                            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.ServicerTenantId))
                            {
                                // Previous implementation to include -1 to servicelink tenant
                                sb.Append("   ( ARRAY_CONTAINS(v.tenant.servicerIds, -1  ) )");
                                sb.Append(" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, -2  ) )");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-1,-2,{FullAccess})) ")); // remove this.
                            }
                            else
                            {
                                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                                sb.Append("   ( ARRAY_CONTAINS(v.tenant.servicerIds, -2  ) )");
                                sb.Append(Invariant($" OR ( ARRAY_CONTAINS(v.tenant.servicerIds, {FullAccess} ))"));
                                sb.Append(Invariant($" OR ( v.tenant.servicer in (-2,{FullAccess})) "));
                            }

                            sb.Append(" OR  ( v.tenant.servicer= " + _userContextService.ServicerTenantId.ToInvariantString() + "  ) ");
                            sb.Append(" OR  ( ARRAY_CONTAINS(v.tenant.servicerIds, " + _userContextService.ServicerTenantId + " ) ) ");
                            sb.AppendLine("  ) ");
                        }
                    }

                    sb.AppendLine(" AND ( ");
                    if (IsRelationshipEntity(parameters))
                    {
                        sb.Append(" (  v.tenant.subContractor IN (-1,-2) AND v.tenant.vendor = 0 ) "); // this is to filter out firms records which are set up independent.
                    }
                    else
                    {
                        sb.Append(" (  v.tenant.subContractor IN (-1,-2)  ) "); // for reference data kind of data.
                    }

                    if (_userContextService.VendorIds != null && _userContextService.VendorIds.Count > 0)
                    {
                        sb.Append(" OR ( v.tenant.subContractor =  "
                            + _userContextService.TenantId
                            + " AND v.tenant.vendor IN ("
                            + string.Join(", ", _userContextService.VendorIds.ToArray())
                            + " ) ) ");
                        sb.Append(" OR ( v.tenant.subContractor  IN (-1,-2)  "
                            + " AND v.tenant.vendor IN ( "
                            + string.Join(", ", _userContextService.VendorIds.ToArray())
                            + " ) ) ");
                    }

                    sb.Append(" OR ( v.tenant.subContractor =  " + _userContextService.TenantId + "  ) ");
                    sb.AppendLine("  ) ");
                    break;
                }

            case "servicer":
                {
                    var servicerTenantIds = new List<long>
                    {
                        _userContextService.TenantId
                    };

                    var associatedServicerList = new List<long>();

                    if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
                    {
                        // Previous implementation to include -1 to servicelink tenant
                        servicerTenantIds.Add(-1);
                        servicerTenantIds.Add(-2);
                        servicerTenantIds.Add(FullAccess);
                        associatedServicerList.Add(-1);
                        associatedServicerList.Add(-2);
                        associatedServicerList.Add(FullAccess);
                    }
                    else
                    {
                        // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                        servicerTenantIds.Add(-2);
                        servicerTenantIds.Add(FullAccess);
                        associatedServicerList.Add(FullAccess);
                    }

                    associatedServicerList.AddRange(_userContextService?.AssociatedServicerTenantIds.OrderBy(i => i) ?? Enumerable.Empty<long>());

                    var servicerIds = string.Join(", ", servicerTenantIds);
                    var associatedServicerIds = string.Join(", ", associatedServicerList);
                    sb.AppendLine(Invariant($"  AND (v.tenant.servicer IN ({servicerIds}) OR EXISTS (SELECT VALUE servicerId FROM servicerId IN v.tenant.servicerIds WHERE servicerId IN ({associatedServicerIds})))"));

                    break;
                }

            default:
                {
                    throw new ArgumentException("Invalid user type");
                }
        }

        string query = sb.Replace("v.", tableOrAliasName).ToString();
        return query;
    }

    /// <summary>
    /// Get Multi-Tenancy where condition for SQL Server DB query.
    /// </summary>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="workorderAlias">Workorder table Alias name.</param>
    /// <returns>Where clause with Multi-Tenancy condition.</returns>
    public string GetSQLWhereClause(string tableAlias, string workorderAlias = null)
    {
        if (!string.IsNullOrEmpty(tableAlias))
        {
            StringBuilder sb = new StringBuilder();

            // only run if user is authenticated.
            if (_userContextService != null && !string.IsNullOrEmpty(_userContextService.Username) && !string.IsNullOrEmpty(_userContextService.TenantType))
            {
                if (_userContextService != null && _userContextService.TenantType != null)
                {
                    switch (_userContextService.TenantType.ToLowerInvariant())
                    {
                        case "masterclient":
                            {
                                // If user belows to one of the below types, he/she should have only visibility to orders created by them respectively.

                                if (!string.IsNullOrEmpty(workorderAlias) &&
                                    string.Equals(_userContextService.SubTenantType, "restrictedclient", StringComparison.OrdinalIgnoreCase))
                                {
                                    sb.AppendLine(" AND (" + workorderAlias + ".OrderCreatorUserId = '" + _userContextService.UserId + "' )");
                                }

                                BuildMasterClientWhereCondition(sb, tableAlias);
                                break;
                            }

                        case "subclient":
                            {
                                BuildSubClientWhereCondition(sb, tableAlias);
                                break;
                            }

                        case "vendor":
                            {
                                BuildVendorWhereCondition(sb, tableAlias);
                                break;
                            }

                        case "subcontractor":
                            {
                                BuildSubContractorWhereCondition(sb, tableAlias);
                                break;
                            }

                        case "servicer":
                            {
                                BuildServicerWhereCondition(sb, tableAlias);
                                break;
                            }

                        default:
                            {
                                throw new ArgumentException("Invalid user type");
                            }
                    }
                }

                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Get Multi-Tenancy where condition for SQL Server DB query.
    /// </summary>
    /// <param name="tableAliases">List of Tables.</param>
    /// <param name="additionalWhereForAliases">Where Condition for each item in the List of Tables.</param>
    /// <param name="workorderAlias">Workorder table Alias name.</param>
    /// <returns>Where clause with Multi-Tenancy condition.</returns>
    public string GetSQLWhereClause(List<string> tableAliases, List<string> additionalWhereForAliases = null, string workorderAlias = null)
    {
        if (tableAliases != null && tableAliases.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            if (_userContextService != null && !string.IsNullOrEmpty(_userContextService.Username) && !string.IsNullOrEmpty(_userContextService.TenantType))
            {
                if (_userContextService != null && _userContextService.TenantType != null)
                {
                    switch (_userContextService.TenantType.ToLowerInvariant())
                    {
                        case "masterclient":
                            {
                                // If user belows to one of the below types, he/she should have only visibility to orders created by them respectively.

                                if (!string.IsNullOrEmpty(workorderAlias) && string.Equals(_userContextService.SubTenantType, "restrictedclient", StringComparison.OrdinalIgnoreCase))
                                {
                                    sb.AppendLine(" AND (" + workorderAlias + ".OrderCreatorUserId = '" + _userContextService.UserId + "') ");
                                }

                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildMasterClientWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildMasterClientWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "subclient":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildSubClientWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildSubClientWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "vendor":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildVendorWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildVendorWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "subcontractor":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildSubContractorWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildSubContractorWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "servicer":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null &&
                                        additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildServicerWhereCondition(sb,
                                            tableAlias,
                                            additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildServicerWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        default:
                            {
                                throw new ArgumentException("Invalid user type");
                            }
                    }
                }

                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Get Multi-Tenancy where condition for SQL Server DB search query.
    /// </summary>
    /// <param name="tableAliases">List of Tables.</param>
    /// <param name="additionalWhereForAliases">Where Condition for each item in the List of Tables.</param>
    /// <param name="workorderAlias">Workorder table Alias name.</param>
    /// <returns>Where clause with Multi-Tenancy condition.</returns>
    public string GetSQLWhereClauseForSearches(List<string> tableAliases, List<string> additionalWhereForAliases = null, string workorderAlias = null)
    {
        if (tableAliases != null && tableAliases.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            if (_userContextService != null && !string.IsNullOrEmpty(_userContextService.Username) && !string.IsNullOrEmpty(_userContextService.TenantType))
            {
                if (_userContextService != null && _userContextService.TenantType != null)
                {
                    switch (_userContextService.TenantType.ToLowerInvariant())
                    {
                        case "masterclient":
                            {
                                // If user belows to one of the below types, he/she should have only visibility to orders created by them respectively.
                                if (!string.IsNullOrEmpty(workorderAlias) && string.Equals(_userContextService.SubTenantType, "restrictedclient", StringComparison.OrdinalIgnoreCase))
                                {
                                    sb.AppendLine(" AND (" + workorderAlias + ".OrderCreatorUserId = '" + _userContextService.UserId + "' )");
                                }

                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildMasterClientWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildMasterClientWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "subclient":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildSubClientWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildSubClientWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "vendor":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildVendorWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildVendorWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "subcontractor":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null && additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildSubContractorWhereCondition(sb, tableAlias, additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildSubContractorWhereCondition(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        case "servicer":
                            {
                                int addWhere = 0;
                                foreach (var tableAlias in tableAliases)
                                {
                                    if (additionalWhereForAliases != null &&
                                        additionalWhereForAliases.Count == tableAliases.Count)
                                    {
                                        BuildServicerWhereConditionForSearches(sb,
                                            tableAlias,
                                            additionalWhereForAliases.ElementAt(addWhere));
                                        addWhere++;
                                    }
                                    else
                                    {
                                        BuildServicerWhereConditionForSearches(sb, tableAlias);
                                    }
                                }

                                break;
                            }

                        default:
                            {
                                throw new ArgumentException("Invalid user type");
                            }
                    }
                }

                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Validate if the Entity has relationships.
    /// </summary>
    /// <param name="parameters">SqlParameterCollection.</param>
    /// <returns>True if entity has relationships.</returns>
    private bool IsRelationshipEntity(SqlParameterCollection parameters)
    {
        string docType = GetDocType(parameters);
        if (!string.IsNullOrEmpty(docType))
        {
            return _needsRelationshipQuery.Where(i => i == docType).FirstOrDefault() != null;
        }

        return false;
    }

    /// <summary>
    /// Update FeedOptions object.
    /// </summary>
    /// <param name="feedOptions">FeedOptions.</param>
    private void UpdateFeedOptions(FeedOptions feedOptions)
    {
        if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
        {
            if (feedOptions == null)
            {
                feedOptions = new FeedOptions() { PopulateQueryMetrics = true };
            }
            else
            {
                feedOptions.PopulateQueryMetrics = true;
            }
        }
    }

    /// <summary>
    /// Get Policy Document Name.
    /// </summary>
    /// <param name="objectWithTenantIds">Entity to apply Multi-Tenancy.</param>
    /// <returns>Multi-Tenancy policy document name.</returns>
    private string GetPolicyDocumentName(object objectWithTenantIds)
    {
        PropertyInfo docType;
        if (typeof(Entities.ITenant).IsAssignableFrom(objectWithTenantIds.GetType()))
        {
            docType = objectWithTenantIds.GetType().GetProperty("EntityName");
        }
        else
        {
            docType = objectWithTenantIds.GetType().GetProperty("CosmosDocType");
        }

        if (docType != null)
        {
            var value = docType.GetValue(objectWithTenantIds);
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                return value.ToString() + PolicyDocumentSuffix;
            }
        }

        return null;
    }

    /// <summary>
    /// Fetch Multi-Tenancy policy document from Cache or Cosmos DB.
    /// </summary>
    /// <param name="policyDocumentName">Multi-Tenancy policy document name.</param>
    /// <returns>Policy File in string format.</returns>
    private async Task<string> FetchPolicy(string policyDocumentName)
    {
        var repositoryOptions = _documentClientAccessor.RepositoryOptions;

        if (!string.IsNullOrEmpty(policyDocumentName))
        {
            if (!string.IsNullOrEmpty(repositoryOptions.PartitionKey))
            {
                var queryOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = false,
                    PartitionKey = new PartitionKey(policyDocumentName)
                };

                UpdateFeedOptions(queryOptions);

                // Build a dynamic query using parameterized SQL
                var queryText =
                    $"SELECT *\n" +
                    $"FROM {repositoryOptions.Collection} p\n" +
                    $"  WHERE (p.cosmosDocType = @cosmosDocType)\n" +
                    $"  AND (p.{repositoryOptions.PartitionKey} = @partKey)\n";

                var parameters = new SqlParameterCollection();
                parameters.Add(new SqlParameter("@cosmosDocType", policyDocumentName));
                parameters.Add(new SqlParameter("@partKey", policyDocumentName));

                var querySpec = new SqlQuerySpec
                {
                    QueryText = queryText,
                    Parameters = parameters,
                };

                var queryable = _documentClientAccessor.DocumentClient.CreateDocumentQuery<Document>(
                    UriFactory.CreateDocumentCollectionUri(repositoryOptions.Database, repositoryOptions.Collection),
                    querySpec,
                    queryOptions).AsDocumentQuery();

                var response = await queryable.ExecuteNextAsync<Document>().ConfigureAwait(false);
                foreach (var doc in response.AsEnumerable())
                {
                    var docText = doc?.ToString();
                    if (!string.IsNullOrEmpty(docText))
                    {
                        _logger.LogDebug($"Retrieved the MultiTenant Policy = {LoggerHelper.SanitizeValue(policyDocumentName)} from database.");
                        return docText;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                _logger.LogDebug($"Shared library did not receive partition key for {repositoryOptions.Endpoint} {repositoryOptions.Collection} .Please address it by correcting the appsettings file.");
            }

            var tenancyPolicy = await LoadPolicyFromTenancyHelperAssembly(policyDocumentName);
            if (string.IsNullOrEmpty(tenancyPolicy))
            {
                tenancyPolicy = await LoadPolicyFromAssembly(policyDocumentName);
            }

            return tenancyPolicy;
        }

        _logger.LogWarning($"Could not retrieve the MultiTenant Policy = {LoggerHelper.SanitizeValue(policyDocumentName)} from database or from resources.");
        return null;
    }

    private async Task<string> LoadPolicyFromTenancyHelperAssembly(string policyDocumentName)
    {
        // Load from tenancy helper assembly.
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Exos.Platform.TenancyHelper.Policies." + policyDocumentName + ".json";
        _logger.LogDebug($"Could not retrieve the MultiTenant Policy = {LoggerHelper.SanitizeValue(policyDocumentName)} from database, trying to retrieve from resources.");
        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            _logger.LogDebug($"Could not retrieve the MultiTenant Policy = {LoggerHelper.SanitizeValue(policyDocumentName)} from database or from resources.");
            return null;
        }

        using StreamReader reader = new StreamReader(stream);
        _logger.LogDebug($"Retrieved the MultiTenant Policy = {LoggerHelper.SanitizeValue(policyDocumentName)} from resources.");
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private async Task<string> LoadPolicyFromAssembly(string policyDocumentName)
    {
        // Load from svc assembly
        string fileContent = null;
        var policyFile = $"TenancyPolicies/{policyDocumentName}.json";
        if (File.Exists(policyFile))
        {
            using StreamReader reader = new StreamReader(policyFile);
            fileContent = await reader.ReadToEndAsync();
        }

        return fileContent;
    }

    /// <summary>
    /// Get Multi-Tenancy policy by Name.
    /// </summary>
    /// <param name="policyName">Policy Name.</param>
    /// <returns>Multi-Tenancy Policy.</returns>
    private async Task<object> GetPolicyObjectByName(string policyName)
    {
        _logger.LogDebug($"Persistence:GetPolicyObjectByName, policyName = {LoggerHelper.SanitizeValue(policyName)}.");

        if (!string.IsNullOrEmpty(policyName))
        {
            var repositoryOptions = _documentClientAccessor.RepositoryOptions;
            var expirationMinutes = repositoryOptions.PolicyTenancyExpirationInMinutes > 0 ? repositoryOptions.PolicyTenancyExpirationInMinutes : 120;

            return await _memoryCache.GetOrCreateAsync(
                $"{repositoryOptions.Collection}{policyName}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes);

                    string policyFromDb = await FetchPolicy(policyName).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(policyFromDb))
                    {
                        return JsonConvert.DeserializeObject(policyFromDb);
                    }

                    return null;
                });
        }

        return null;
    }

    /// <summary>
    /// Build Where Condition for MasterClient tenant type.
    /// </summary>
    /// <param name="sb">String Builder to append the condition.</param>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="additionalWhere">Additional where condition.</param>
    private void BuildMasterClientWhereCondition(StringBuilder sb, string tableAlias, string additionalWhere = null)
    {
        sb.AppendLine(" AND (");
        if (!string.IsNullOrEmpty(additionalWhere))
        {
            sb.Append(additionalWhere + " ");
        }

        // restrictedclient client user can work across multiple sub clients and master clients.
        if (string.Equals(_userContextService.SubTenantType, "restrictedclient", StringComparison.OrdinalIgnoreCase) || string.Equals(_userContextService.SubTenantType, "restrictedmasterclient", StringComparison.OrdinalIgnoreCase))
        {
            if (_userContextService.SubClientIds != null && _userContextService.SubClientIds.Count > 0 && _userContextService.MasterClientIds != null && _userContextService.MasterClientIds.Count > 0)
            {
                sb.Append(" ( ( " + tableAlias + ".ClientTenantId IN (-1,-2, " + string.Join(",", _userContextService.MasterClientIds.ToArray()) + " ) )");
                sb.Append(" AND (" + " (" + tableAlias + ".SubClientTenantId IN (-1,-2," + string.Join(",", _userContextService.SubClientIds.ToArray()) + " )))) ");
            }
            else
            {
                sb.Append(" ( ( " + tableAlias + ".ClientTenantId IN (-1,-2) AND " + tableAlias + ".SubClientTenantId IN (-1,-2))");
                sb.Append(" OR (" + tableAlias + ".ClientTenantId = " + _userContextService.TenantId + " ) ) ");
            }
        }
        else
        {
            if (_userContextService.SubClientIds != null && _userContextService.SubClientIds.Count > 0)
            {
                sb.Append(" ( ( " + tableAlias + ".ClientTenantId = " + _userContextService.TenantId + ")");
                sb.Append(" OR (" + tableAlias + ".ClientTenantId IN (-1,-2)" + " AND (" + tableAlias + ".SubClientTenantId IN (-1,-2," + string.Join(",", _userContextService.SubClientIds.ToArray()) + " )))) ");
            }
            else
            {
                sb.Append(" ( ( " + tableAlias + ".ClientTenantId IN (-1,-2) AND " + tableAlias + ".SubClientTenantId IN (-1,-2))");
                sb.Append(" OR (" + tableAlias + ".ClientTenantId = " + _userContextService.TenantId + " ) ) ");
            }
        }

        AppendAssociatedServicerWhereCondition(sb, tableAlias);

        sb.AppendLine(")");
    }

    /// <summary>
    /// Build Where Condition for SubClient tenant type.
    /// </summary>
    /// <param name="sb">String Builder to append the condition.</param>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="additionalWhere">Additional where condition.</param>
    private void BuildSubClientWhereCondition(StringBuilder sb, string tableAlias, string additionalWhere = null)
    {
        sb.AppendLine(" AND (");
        if (!string.IsNullOrEmpty(additionalWhere))
        {
            sb.Append(additionalWhere + " ");
        }

        if (_userContextService.MasterClientIds != null && _userContextService.MasterClientIds.Count > 0)
        {
            sb.Append(" ( ( " + tableAlias + ".SubClientTenantId = " + _userContextService.TenantId + ")");
            sb.Append(" OR (" + tableAlias + ".SubClientTenantId IN (-1,-2)" + " AND (" + tableAlias + ".ClientTenantId IN (-1,-2," + string.Join(",", _userContextService.MasterClientIds.ToArray()) + " )))) ");
        }
        else
        {
            sb.Append(" ( ( " + tableAlias + ".SubClientTenantId IN (-1,-2) AND " + tableAlias + ".ClientTenantId IN (-1,-2))");
            sb.Append(" OR (" + tableAlias + ".SubClientTenantId = " + _userContextService.TenantId + " ) )");
        }

        AppendAssociatedServicerWhereCondition(sb, tableAlias);

        sb.AppendLine(")");
    }

    /// <summary>
    /// Build Where Condition for Vendor tenant type.
    /// </summary>
    /// <param name="sb">String Builder to append the condition.</param>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="additionalWhere">Additional where condition.</param>
    private void BuildVendorWhereCondition(StringBuilder sb, string tableAlias, string additionalWhere = null)
    {
        sb.AppendLine(" AND (");

        if (!string.IsNullOrEmpty(additionalWhere))
        {
            sb.Append(additionalWhere + " ");
        }

        if (_userContextService.SubVendorIds != null && _userContextService.SubVendorIds.Count > 0)
        {
            sb.Append("( (" + tableAlias + ".VendorTenantId = " + _userContextService.TenantId + ")");
            sb.Append(" OR (" + tableAlias + ".VendorTenantId IN (-1,-2)" + " AND (" + tableAlias + ".SubContractorTenantId IN (-1,-2," + string.Join(",", _userContextService.SubVendorIds.ToArray()) + "))))");
        }
        else
        {
            sb.Append(" ( ( " + tableAlias + ".VendorTenantId IN (-1,-2) AND " + tableAlias + ".SubContractorTenantId IN (-1,-2))");
            sb.Append(" OR (" + tableAlias + ".VendorTenantId = " + _userContextService.TenantId + ")) ");
        }

        AppendAssociatedServicerWhereCondition(sb, tableAlias);

        sb.AppendLine(")");
    }

    /// <summary>
    /// Build Where Sub-Contractor for MasterClient tenant type.
    /// </summary>
    /// <param name="sb">String Builder to append the condition.</param>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="additionalWhere">Additional where condition.</param>
    private void BuildSubContractorWhereCondition(StringBuilder sb, string tableAlias, string additionalWhere = null)
    {
        sb.AppendLine(" AND (");
        if (!string.IsNullOrEmpty(additionalWhere))
        {
            sb.Append(additionalWhere + " ");
        }

        if (_userContextService.VendorIds != null && _userContextService.VendorIds.Count > 0)
        {
            sb.Append(" ( (" + tableAlias + ".SubContractorTenantId = " + _userContextService.TenantId + ")");
            sb.Append(" OR (" + tableAlias + ".SubContractorTenantId IN (-1,-2)" + " AND (" + tableAlias + ".VendorTenantId IN (-1,-2," + string.Join(", ", _userContextService.VendorIds.ToArray()) + "))) )");
        }
        else
        {
            sb.Append(" ( (" + tableAlias + ".SubContractorTenantId IN (-1,-2) AND " + tableAlias + ".VendorTenantId IN (-1,-2))");
            sb.Append(" OR (" + tableAlias + ".SubContractorTenantId = " + _userContextService.TenantId + ") ) ");
        }

        AppendAssociatedServicerWhereCondition(sb, tableAlias);

        sb.AppendLine(")");
    }

    /// <summary>
    /// Build Where Condition for Servicer tenant type.
    /// </summary>
    /// <param name="sb">String Builder to append the condition.</param>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="additionalWhere">Additional where condition.</param>
    private void BuildServicerWhereCondition(StringBuilder sb, string tableAlias, string additionalWhere = null)
    {
        sb.AppendLine(" AND (");

        if (!string.IsNullOrEmpty(additionalWhere))
        {
            // sb.Append(additionalWhere + " ");
            sb.Append(Invariant($"{additionalWhere} "));
        }

        string servicerGroupTenantId = GetServicerGroupTenantId();
        if (!string.IsNullOrEmpty(servicerGroupTenantId))
        {
            // sb.Append(" (" + tableAlias + ".ServicerTenantId = " + _userContextService.TenantId + " AND (" + tableAlias + servicerGroupTenantId + "))");

            sb.Append(Invariant($"({tableAlias}.ServicerTenantId = {_userContextService.TenantId} AND ({tableAlias}{servicerGroupTenantId}))"));

            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
            {
                // Previous implementation to include -1 to servicelink tenant
                // sb.Append(" OR (" + tableAlias + ".ServicerTenantId IN (-1,-2) AND " + tableAlias + servicerGroupTenantId + ")");

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId IN (-1,-2,{FullAccess}) AND {tableAlias}{servicerGroupTenantId})"));
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId IN (-2,{FullAccess}) AND {tableAlias}{servicerGroupTenantId})"));
            }
        }
        else
        {
            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
            {
                // Previous implementation to include -1 to servicelink tenant

                // sb.Append(" (" + tableAlias + ".ServicerTenantId IN (-1,-2) AND " + tableAlias + ".ServicerGroupTenantId IN (-1,-2))");

                sb.Append(Invariant($"({tableAlias}.ServicerTenantId IN (-1,-2,{FullAccess}) AND {tableAlias}.ServicerGroupTenantId IN (-1,-2,{FullAccess}))"));

                // sb.Append(" OR (" + tableAlias + ".ServicerTenantId = " + _userContextService.TenantId + " AND " + tableAlias + ".ServicerGroupTenantId IN (-1,-2))");

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId = {_userContextService.TenantId} AND {tableAlias}.ServicerGroupTenantId IN (-1,-2,{FullAccess}))"));
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                sb.Append(Invariant($"({tableAlias}.ServicerTenantId IN (-2,{FullAccess}) AND {tableAlias}.ServicerGroupTenantId IN (-2,{FullAccess}))"));

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId = {_userContextService.TenantId} AND {tableAlias}.ServicerGroupTenantId IN (-2,{FullAccess}))"));
            }
        }

        sb.AppendLine(")");
    }

    /// <summary>
    /// Build Search where Condition for Servicer tenant type.
    /// </summary>
    /// <param name="sb">String Builder to append the condition.</param>
    /// <param name="tableAlias">Table name.</param>
    /// <param name="additionalWhere">Additional where condition.</param>
    private void BuildServicerWhereConditionForSearches(StringBuilder sb, string tableAlias, string additionalWhere = null)
    {
        sb.AppendLine(" AND (");
        if (!string.IsNullOrEmpty(additionalWhere))
        {
            // sb.Append(additionalWhere + " ");
            sb.Append(Invariant($"{additionalWhere} "));
        }

        string servicerGroupTenantId = GetServicerGroupTenantId();
        if (!string.IsNullOrEmpty(servicerGroupTenantId))
        {
            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
            {
                // Previous implementation to include -1 to servicelink tenant

                // sb.Append(" (" + tableAlias + ".ServicerTenantId IN ( " + string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray()) + " ) AND (" + tableAlias + servicerGroupTenantId + "))");

                sb.Append(Invariant($" ({tableAlias}.ServicerTenantId IN ( {string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray())} ) AND ({tableAlias}{servicerGroupTenantId}))"));

                // sb.Append(" OR (" + tableAlias + ".ServicerTenantId IN (-1,-2) AND " + tableAlias + servicerGroupTenantId + ")");

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId IN (-1,-2,{FullAccess}) AND {tableAlias}{servicerGroupTenantId})"));
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                sb.Append(Invariant($" ({tableAlias}.ServicerTenantId IN ( {string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray())} ) AND ({tableAlias}{servicerGroupTenantId}))"));

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId (-2,{FullAccess}) AND {tableAlias}{servicerGroupTenantId})"));
            }
        }
        else
        {
            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
            {
                // Previous implementation to include -1 to servicelink tenant
                // sb.Append(" (" + tableAlias + ".ServicerTenantId IN (-1,-2) AND " + tableAlias + ".ServicerGroupTenantId IN (-1,-2))");

                sb.Append(Invariant($" ({tableAlias}.ServicerTenantId IN (-1,-2,{FullAccess}) AND {tableAlias}.ServicerGroupTenantId IN (-1,-2,{FullAccess}))"));

                // sb.Append(" OR (" + tableAlias + ".ServicerTenantId IN ( " + string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray()) + " ) AND " + tableAlias + ".ServicerGroupTenantId IN (-1,-2))");

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId IN ( {string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray())} ) AND {tableAlias}.ServicerGroupTenantId IN (-1,-2,{FullAccess}))"));
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                sb.Append(Invariant($" ({tableAlias}.ServicerTenantId IN (-2,{FullAccess}) AND {tableAlias}.ServicerGroupTenantId IN (-2,{FullAccess}))"));

                sb.Append(Invariant($" OR ({tableAlias}.ServicerTenantId IN ( {string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray())} ) AND {tableAlias}.ServicerGroupTenantId IN (-2,{FullAccess}))"));
            }
        }

        sb.AppendLine(")");
    }

    /// <summary>
    /// Get Servicer Group Tenant Id.
    /// </summary>
    /// <returns>Servicer Group Tenant Id.</returns>
    private string GetServicerGroupTenantId()
    {
        string servicerGroupTenantId = string.Empty;
        var userContext = _userContextService.GetUserContext();
        if (userContext.ServicerGroupTenantId != 0)
        {
            // servicerGroupTenantId = ".ServicerGroupTenantId IN (-1,-2," + userContext.ServicerGroupTenantId.ToString(CultureInfo.InvariantCulture) + ")";

            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
            {
                // Previous implementation to include -1 to servicelink tenant
                servicerGroupTenantId = $".ServicerGroupTenantId IN (-1,-2,{FullAccess},{userContext.ServicerGroupTenantId.ToInvariantString()})";
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                servicerGroupTenantId = $".ServicerGroupTenantId IN (-2,{FullAccess},{userContext.ServicerGroupTenantId.ToInvariantString()})";
            }
        }
        else
        {
            if (_userContextService.ServicerGroups != null && _userContextService.ServicerGroups.Any())
            {
                // servicerGroupTenantId = ".ServicerGroupTenantId IN (-1,-2," + string.Join(",", _userContextService.ServicerGroups.ToArray()) + ")";
                if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.TenantId))
                {
                    // Previous implementation to include -1 to servicelink tenant
                    servicerGroupTenantId = $".ServicerGroupTenantId IN (-1,-2,{FullAccess},{string.Join(",", _userContextService.ServicerGroups.ToArray())})";
                }
                else
                {
                    // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                    servicerGroupTenantId = $".ServicerGroupTenantId IN (-2,{FullAccess},{string.Join(",", _userContextService.ServicerGroups.ToArray())})";
                }
            }
        }

        return servicerGroupTenantId;
    }

    private void AppendAssociatedServicerWhereCondition(StringBuilder sb, string tableAlias)
    {
        if (_userContextService.AssociatedServicerTenantIds != null && _userContextService.AssociatedServicerTenantIds.Count > 0)
        {
            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.ServicerTenantId))
            {
                // Previous implementation to include -1 to servicelink tenant
                sb.AppendLine(" AND ( ");
                sb.Append($" ( {tableAlias}.ServicerTenantId IN (-1,-2,{FullAccess}," + string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray()) + " ) ) ");
                sb.AppendLine(" ) ");
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                sb.AppendLine(" AND ( ");
                sb.Append(Invariant($" ({tableAlias}.ServicerTenantId IN (-2,{FullAccess},{string.Join(",", _userContextService.AssociatedServicerTenantIds.ToArray())})) "));
                sb.AppendLine(" ) ");
            }
        }
        else
        {
            if (_platformDefaultOptions.ServiceLinkTenantIds.Contains(_userContextService.ServicerTenantId))
            {
                // Previous implementation to include -1 to servicelink tenant
                sb.AppendLine(" AND ( ");
                sb.Append(Invariant($"({tableAlias}.ServicerTenantId IN (-1,-2,{FullAccess}) )"));
                sb.AppendLine(" ) ");
            }
            else
            {
                // Tenants that are not servicelink or not included in the list can read data with -1 in tenancy.
                sb.AppendLine(" AND ( ");
                sb.Append(Invariant($"({tableAlias}.ServicerTenantId IN (-2,{FullAccess}) )"));
                sb.AppendLine(" ) ");
            }
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA1502 // Avoid excessive complexity
