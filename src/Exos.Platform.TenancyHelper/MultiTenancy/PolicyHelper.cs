#pragma warning disable SA1402 // FileMayOnlyContainASingleType
namespace Exos.Platform.TenancyHelper.MultiTenancy
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Security;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl;
    using Exos.Platform.TenancyHelper.PersistenceService;
    using Microsoft.Azure.Documents;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class PolicyHelper : IPolicyHelper
    {
        private readonly IDocumentClientAccessor _documentClientAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly PolicyHelperMgr _policyHelperMgr;
        private readonly ILogger _logger;
        private readonly IUserContextService _userContextService;
        private readonly PlatformDefaultsOptions _platformDefaultOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelper"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userContextService">The user context service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelper(
            IDocumentClientAccessor documentClientAccessor,
            IMemoryCache memoryCache,
            ILogger<PolicyHelper> logger,
            IUserContextService userContextService,
            IOptions<PlatformDefaultsOptions> platformDefaultOptions)
        {
            ArgumentNullException.ThrowIfNull(memoryCache);

            _documentClientAccessor = documentClientAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
            _userContextService = userContextService;
            _platformDefaultOptions = platformDefaultOptions?.Value;

            _policyHelperMgr = new PolicyHelperMgr(_documentClientAccessor, _memoryCache, _logger, _userContextService, _platformDefaultOptions);
        }

        /// <inheritdoc/>
        public string GetSQLWhereClause(List<string> tableAliases, List<string> additionalWhereForAliases = null, string workorderAlias = null)
        {
            return _policyHelperMgr.GetSQLWhereClause(tableAliases, additionalWhereForAliases, workorderAlias);
        }

        /// <inheritdoc/>
        public string GetSQLWhereClauseForSearches(List<string> tableAliases, List<string> additionalWhereForAliases = null, string workorderAlias = null)
        {
            return _policyHelperMgr.GetSQLWhereClauseForSearches(tableAliases, additionalWhereForAliases, workorderAlias);
        }

        /// <inheritdoc/>
        public string GetSQLWhereClause(string tableAlias, string workorderAlias = null)
        {
            return _policyHelperMgr.GetSQLWhereClause(tableAlias, workorderAlias);
        }

        /// <inheritdoc/>
        public Dictionary<string, int> GetTenantIdsForInsert(object objectWithTenantIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Dictionary<string, int> GetTenantIdsForUpdate(object objectWithTenantIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> IsEntityMultiTenant(IPolicyContext policyContext)
        {
            return _policyHelperMgr.IsEntityMultiTenant(policyContext);
        }

        /// <inheritdoc/>
        public Task<EntityPolicyAttributes> ReadEntityPolicyAttributes(IPolicyContext policyContext)
        {
            return _policyHelperMgr.ReadEntityPolicyAttributes(policyContext);
        }

        /// <inheritdoc/>
        public bool IsObjectUpdatableByTenant(object objectWithTenantIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool IsTenantIdsUpdatable()
        {
            return _policyHelperMgr.IsTenantIdsUpdatable(null);
        }

        /// <inheritdoc/>
        public Task<object> SetTenantIdsForInsert(object objectWithTenantIds, IPolicyContext policyContext = null)
        {
            return _policyHelperMgr.HandleInsert(objectWithTenantIds, policyContext);
        }

        /// <inheritdoc/>
        public Task<object> SetTenantIdsForUpdate(object objectWithTenantIds, IPolicyContext policyContext = null)
        {
            return _policyHelperMgr.HandleUpdate(objectWithTenantIds, policyContext);
        }

        /// <inheritdoc/>
        public string GetCosmosWhereClause(string tableOrAliasName, SqlParameterCollection parameters, EntityPolicyAttributes entityPolicyAttributes, IPolicyContext policyContext = null)
        {
            return _policyHelperMgr.GetCosmosWhereClause(tableOrAliasName, parameters, entityPolicyAttributes);
        }

        /// <inheritdoc/>
        public string GetCosmosWhereClauseForSearches(string tableOrAliasName, SqlParameterCollection parameters, EntityPolicyAttributes entityPolicyAttributes, IPolicyContext policyContext = null)
        {
            return _policyHelperMgr.GetCosmosWhereClauseForSearches(tableOrAliasName, parameters, entityPolicyAttributes);
        }

        /// <inheritdoc/>
        public string GetDocType(SqlParameterCollection parameters)
        {
            return _policyHelperMgr.GetDocType(parameters);
        }
    }

    /// <inheritdoc/>
    public class PolicyHelperInstance1 : PolicyHelper, IPolicyHelperInstance1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelperInstance1"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userCtxService">The user ctx service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelperInstance1(IDocumentClientAccessor1 documentClientAccessor, IMemoryCache memoryCache, ILogger<PolicyHelperInstance1> logger, IUserContextService userCtxService, IOptions<PlatformDefaultsOptions> platformDefaultOptions) : base(documentClientAccessor, memoryCache, logger, userCtxService, platformDefaultOptions)
        {
        }
    }

    /// <inheritdoc/>
    public class PolicyHelperInstance2 : PolicyHelper, IPolicyHelperInstance2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelperInstance2"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userCtxService">The user ctx service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelperInstance2(IDocumentClientAccessor2 documentClientAccessor, IMemoryCache memoryCache, ILogger<PolicyHelperInstance2> logger, IUserContextService userCtxService, IOptions<PlatformDefaultsOptions> platformDefaultOptions) : base(documentClientAccessor, memoryCache, logger, userCtxService, platformDefaultOptions)
        {
        }
    }

    /// <inheritdoc/>
    public class PolicyHelperInstance3 : PolicyHelper, IPolicyHelperInstance3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelperInstance3"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userCtxService">The user ctx service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelperInstance3(IDocumentClientAccessor4 documentClientAccessor, IMemoryCache memoryCache, ILogger<PolicyHelperInstance4> logger, IUserContextService userCtxService, IOptions<PlatformDefaultsOptions> platformDefaultOptions) : base(documentClientAccessor, memoryCache, logger, userCtxService, platformDefaultOptions)
        {
        }
    }

    /// <inheritdoc/>
    public class PolicyHelperInstance4 : PolicyHelper, IPolicyHelperInstance4
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelperInstance4"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userCtxService">The user ctx service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelperInstance4(IDocumentClientAccessor4 documentClientAccessor, IMemoryCache memoryCache, ILogger<PolicyHelperInstance4> logger, IUserContextService userCtxService, IOptions<PlatformDefaultsOptions> platformDefaultOptions) : base(documentClientAccessor, memoryCache, logger, userCtxService, platformDefaultOptions)
        {
        }
    }

    /// <inheritdoc/>
    public class PolicyHelperInstance5 : PolicyHelper, IPolicyHelperInstance5
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelperInstance5"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userCtxService">The user ctx service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelperInstance5(IDocumentClientAccessor5 documentClientAccessor, IMemoryCache memoryCache, ILogger<PolicyHelperInstance5> logger, IUserContextService userCtxService, IOptions<PlatformDefaultsOptions> platformDefaultOptions) : base(documentClientAccessor, memoryCache, logger, userCtxService, platformDefaultOptions)
        {
        }
    }

    /// <inheritdoc/>
    public class PolicyHelperInstance6 : PolicyHelper, IPolicyHelperInstance6
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyHelperInstance6"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">The document client accessor.</param>
        /// <param name="memoryCache">An <see cref="IMemoryCache" /> instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userCtxService">The user ctx service.</param>
        /// <param name="platformDefaultOptions">The platform default options.</param>
        public PolicyHelperInstance6(IDocumentClientAccessor6 documentClientAccessor, IMemoryCache memoryCache, ILogger<PolicyHelperInstance6> logger, IUserContextService userCtxService, IOptions<PlatformDefaultsOptions> platformDefaultOptions) : base(documentClientAccessor, memoryCache, logger, userCtxService, platformDefaultOptions)
        {
        }
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
