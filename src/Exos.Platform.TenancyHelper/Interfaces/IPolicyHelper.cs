namespace Exos.Platform.TenancyHelper.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.TenancyHelper.MultiTenancy;
    using Microsoft.Azure.Documents;

    /// <summary>
    /// Interface for helper methods for Multi-tenant policy.
    /// </summary>
    public interface IPolicyHelper
    {
        /// <summary>
        /// Get the  SQL where condition with the tenancy filter applied.
        /// </summary>
        /// <param name="tableAliases">Table Aliases.</param>
        /// <param name="additionalWhereForAliases">Additional Where condition.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>SQL where condition with the tenancy filter applied.</returns>
        string GetSQLWhereClause(List<string> tableAliases, List<string> additionalWhereForAliases = null, string workorderAlias = null);

        /// <summary>
        /// Get the SQL where condition with the tenancy filter applied.
        /// </summary>
        /// <param name="tableAlias">Table Alias.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>SQL where condition with the tenancy filter applied.</returns>
        string GetSQLWhereClause(string tableAlias, string workorderAlias = null);

        /// <summary>
        /// Get the SQL where condition with the tenancy filter applied.
        /// This adds IN clause for Servicer TenantId instead of equals.
        /// </summary>
        /// <param name="tableAliases">Table Aliases.</param>
        /// <param name="additionalWhereForAliases">Additional Where condition.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>SQL where condition with the tenancy filter applied.</returns>
        string GetSQLWhereClauseForSearches(List<string> tableAliases, List<string> additionalWhereForAliases = null, string workorderAlias = null);

        /// <summary>
        /// Get the Cosmos where condition with the tenancy filter applied.
        /// </summary>
        /// <param name="tableOrAliasName">Table Alias.</param>
        /// <param name="parameters">Query Parameters.</param>
        /// <param name="entityPolicyAttributes">Entity Policy.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Cosmos where condition with the tenancy filter applied.</returns>
        string GetCosmosWhereClause(string tableOrAliasName, SqlParameterCollection parameters, EntityPolicyAttributes entityPolicyAttributes, IPolicyContext policyContext = null);

        /// <summary>
        /// Get the Cosmos where condition with the tenancy filter applied.
        /// </summary>
        /// <param name="tableOrAliasName">Table Alias.</param>
        /// <param name="parameters">Query Parameters.</param>
        /// <param name="entityPolicyAttributes">Entity Policy.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Cosmos where condition with the tenancy filter applied.</returns>
        string GetCosmosWhereClauseForSearches(string tableOrAliasName, SqlParameterCollection parameters, EntityPolicyAttributes entityPolicyAttributes, IPolicyContext policyContext = null);

        /// <summary>
        /// Set the Tenant Ids values for insert.
        /// </summary>
        /// <param name="objectWithTenantIds">Object to insert.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Object with Tenant Ids.</returns>
        Task<object> SetTenantIdsForInsert(object objectWithTenantIds, IPolicyContext policyContext = null);

        /// <summary>
        /// /// Get the Tenant Ids values for insert.
        /// </summary>
        /// <param name="objectWithTenantIds">>Object to insert.</param>
        /// <returns>List of Tenant Id's.</returns>
        Dictionary<string, int> GetTenantIdsForInsert(object objectWithTenantIds);

        /// <summary>
        /// Check if tenant id can update the object.
        /// </summary>
        /// <returns>Return if Tenant Id can update the object.</returns>
        bool IsTenantIdsUpdatable();

        /// <summary>
        /// Check if Entity is available for multi-tenancy.
        /// </summary>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>True if Entity is available for multi-tenancy.</returns>
        Task<bool> IsEntityMultiTenant(IPolicyContext policyContext);

        /// <summary>
        /// Read Tenancy Policy Attributes.
        /// </summary>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>EntityPolicyAttributes.</returns>
        Task<EntityPolicyAttributes> ReadEntityPolicyAttributes(IPolicyContext policyContext);

        /// <summary>
        /// Check if Tenant can update the object.
        /// </summary>
        /// <param name="objectWithTenantIds">Object to update.</param>
        /// <returns>True if object can be updated for the tenant.</returns>
        bool IsObjectUpdatableByTenant(object objectWithTenantIds);

        /// <summary>
        /// Set the Tenant Id values for update.
        /// </summary>
        /// <param name="objectWithTenantIds">Object to update.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Object with Tenant Id applied.</returns>
        Task<object> SetTenantIdsForUpdate(object objectWithTenantIds, IPolicyContext policyContext = null);

        /// <summary>
        /// Get the Tenant Id values for update.
        /// </summary>
        /// <param name="objectWithTenantIds">Object to update.</param>
        /// <returns>Tenant Id Values.</returns>
        Dictionary<string, int> GetTenantIdsForUpdate(object objectWithTenantIds);

        /// <summary>
        /// Get the document type.
        /// </summary>
        /// <param name="parameters">SqlParameterCollection.</param>
        /// <returns>Document Type value.</returns>
        string GetDocType(SqlParameterCollection parameters);
    }

    /// <inheritdoc/>
    public interface IPolicyHelperInstance1 : IPolicyHelper
    {
    }

    /// <inheritdoc/>
    public interface IPolicyHelperInstance2 : IPolicyHelper
    {
    }

    /// <inheritdoc/>
    public interface IPolicyHelperInstance3 : IPolicyHelper
    {
    }

    /// <inheritdoc/>
    public interface IPolicyHelperInstance4 : IPolicyHelper
    {
    }

    /// <inheritdoc/>
    public interface IPolicyHelperInstance5 : IPolicyHelper
    {
    }

    /// <inheritdoc/>
    public interface IPolicyHelperInstance6 : IPolicyHelper
    {
    }
}
