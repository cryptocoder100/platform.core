#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1307 // Specify StringComparison
#pragma warning disable CA1502 // Avoid excessive complexity
#pragma warning disable CA1506 // Avoid excessive class coupling
#pragma warning disable CA1724 // Type names should not match namespaces
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1402 // FileMayOnlyContainASingleType

namespace Exos.Platform.TenancyHelper.PersistenceService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.Models;
    using Exos.Platform.TenancyHelper.MultiTenancy;
    using Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl;
    using Exos.Platform.TenancyHelper.Utils;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using ReflectionHelper = Exos.Platform.AspNetCore.Helpers.ReflectionHelper;

    /// <inheritdoc/>
    public class PersistenceService : IPersistenceService
    {
        private readonly IDocumentClientAccessor _documentClientAccessor;
        private readonly IDistributedCache _distributedCache;
        private readonly IUserContextService _userContextService;
        private readonly IPolicyContext _policyContext;
        private readonly IPolicyHelper _policyHelper;
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceService"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">DocumentClientAccessor.</param>
        /// <param name="distributedCache">DistributedCache.</param>
        /// <param name="userContextService">UserContextService.</param>
        /// <param name="policyHelper">PolicyHelper.</param>
        /// <param name="policyContext">PolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceService(
            IDocumentClientAccessor documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelper policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceService> logger,
            TelemetryClient telemetryClient)
        {
            _documentClientAccessor = documentClientAccessor ?? throw new ArgumentNullException(nameof(documentClientAccessor));
            _distributedCache = distributedCache;
            _policyHelper = policyHelper;
            _userContextService = userContextService;
            _policyContext = policyContext;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        /// <inheritdoc/>
        public IRepositoryOptions RepositoryOptions => _documentClientAccessor.RepositoryOptions;

        /// <inheritdoc/>
        public IPolicyHelper PolicyHelper => _policyHelper;

        /// <inheritdoc/>
        public Task<ResourceResponse<Document>> CreateDocumentAsync(BaseModel document)
        {
            return CreateDocumentAsync(document, _policyContext);
        }

        /// <inheritdoc/>
        public async Task<ResourceResponse<Document>> CreateDocumentAsync(BaseModel document, IPolicyContext policyContext)
        {
            if (document != null)
            {
                document.Audit = new Exos.Platform.TenancyHelper.Models.AuditModel()
                {
                    CreatedBy = _userContextService.UserId,
                    CreatedDate = DateTimeOffset.UtcNow,
                    IsDeleted = false,
                    LastUpdatedBy = _userContextService.UserId,
                    LastUpdatedDate = DateTimeOffset.UtcNow,
                };
                if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy)
                {
                    await _policyHelper.SetTenantIdsForInsert(document, policyContext).ConfigureAwait(false);
                }

                return await _documentClientAccessor.DocumentClient.CreateDocumentAsync(
                       UriFactory.CreateDocumentCollectionUri(_documentClientAccessor.RepositoryOptions.Database, _documentClientAccessor.RepositoryOptions.Collection), document).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentNullException(nameof(document), "Document cannot be null");
            }
        }

        /// <inheritdoc/>
        public Task<ResourceResponse<Document>> ReplaceDocumentAsync(BaseModel docToReplace, IPolicyContext policyContext = null)
        {
            return ReplaceDocumentAsync(null, docToReplace, policyContext ?? _policyContext);
        }

        /// <inheritdoc/>
        public async Task<ResourceResponse<Document>> ReplaceDocumentAsync(string id, BaseModel docToReplace, IPolicyContext policyContext = null)
        {
            if (docToReplace == null)
            {
                throw new ArgumentNullException(nameof(docToReplace));
            }

            if (docToReplace.Audit != null)
            {
                if (_userContextService != null && _userContextService.UserId != null)
                {
                    docToReplace.Audit.LastUpdatedBy = _userContextService.UserId;
                }

                docToReplace.Audit.LastUpdatedDate = DateTimeOffset.UtcNow;
            }

            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy)
            {
                await _policyHelper.SetTenantIdsForUpdate(docToReplace, policyContext == null ? _policyContext : policyContext).ConfigureAwait(false);
            }

            if (_documentClientAccessor.RepositoryOptions.ForceConcurrencyCheck && string.IsNullOrEmpty(docToReplace._Etag))
            {
                throw new ArgumentException("Optimistic concurrency is enforced and application did not receive any Version/ETag info.");
            }

            // will be removed once everyone is upgraded to use version.
            if (!string.IsNullOrEmpty(docToReplace._Etag))
            {
                // enable Concurrency Check  on version sent to UI
                var ac = new AccessCondition { Condition = docToReplace._Etag, Type = AccessConditionType.IfMatch };

                // Replace
                var now = DateTimeOffset.UtcNow;
                var watch = Stopwatch.StartNew();
                var response = await _documentClientAccessor.DocumentClient.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(
                        _documentClientAccessor.RepositoryOptions.Database,
                        _documentClientAccessor.RepositoryOptions.Collection,
                        string.IsNullOrEmpty(id) ? docToReplace.Id : id),
                    docToReplace,
                    new RequestOptions { AccessCondition = ac }).ConfigureAwait(false);
                watch.Stop();
                if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                {
                    TrackQuery(
                        now,
                        watch.Elapsed,
                        response.RequestCharge,
                        "ReplaceDocumentAsync",
                        _telemetryClient,
                        response.ContentLocation,
                        null,
                        null,
                        response.ActivityId,
                        response.RequestDiagnosticsString,
                        isContinuation: false,
                        resultCount: null);
                }

                return response;
            }
            else
            {
                var now = DateTimeOffset.UtcNow;
                var watch = Stopwatch.StartNew();
                var response = await _documentClientAccessor.DocumentClient.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(
                        _documentClientAccessor.RepositoryOptions.Database,
                        _documentClientAccessor.RepositoryOptions.Collection,
                        docToReplace.Id),
                    docToReplace).ConfigureAwait(false);
                watch.Stop();
                if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                {
                    TrackQuery(
                        now,
                        watch.Elapsed,
                        response.RequestCharge,
                        "ReplaceDocumentAsync",
                        _telemetryClient,
                        response.ContentLocation,
                        null,
                        null,
                        response.ActivityId,
                        response.RequestDiagnosticsString,
                        isContinuation: false,
                        resultCount: null);
                }

                return response;
            }
        }

        /// <inheritdoc/>
        public async Task<ResourceResponse<Document>> ReplaceDocumentNative(string id, dynamic docToReplace, IPolicyContext policyContext = null)
        {
            if (docToReplace.audit != null)
            {
                if (_userContextService != null && _userContextService.UserId != null)
                {
                    docToReplace.audit.lastUpdatedBy = _userContextService.UserId;
                }

                docToReplace.audit.lastUpdatedDate = DateTimeOffset.UtcNow;
            }

            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy)
            {
                await _policyHelper.SetTenantIdsForUpdate(docToReplace, policyContext ?? _policyContext);
            }

            if (_documentClientAccessor.RepositoryOptions.ForceConcurrencyCheck && string.IsNullOrEmpty(docToReplace._etag))
            {
                throw new ArgumentException("Optimistic concurrency is enforced and application did not receive any Version/ETag info.");
            }

            // Will be removed once everyone is upgraded to use version.
            if (!string.IsNullOrEmpty(docToReplace._etag))
            {
                // Enable Concurrency Check  on version sent to UI
                var ac = new AccessCondition { Condition = docToReplace._etag, Type = AccessConditionType.IfMatch };

                // Replace
                var now = DateTimeOffset.UtcNow;
                var watch = Stopwatch.StartNew();
                var response = await _documentClientAccessor.DocumentClient.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(
                        _documentClientAccessor.RepositoryOptions.Database,
                        _documentClientAccessor.RepositoryOptions.Collection,
                        string.IsNullOrEmpty(id) ? docToReplace.id : id),
                    docToReplace,
                    new RequestOptions { AccessCondition = ac });
                watch.Stop();
                if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                {
                    TrackQuery(
                        now,
                        watch.Elapsed,
                        response.RequestCharge,
                        "ReplaceDocumentAsync",
                        _telemetryClient,
                        response.ContentLocation,
                        null,
                        null,
                        response.ActivityId,
                        response.RequestDiagnosticsString,
                        isContinuation: false,
                        resultCount: null);
                }

                return response;
            }
            else
            {
                var now = DateTimeOffset.UtcNow;
                var watch = Stopwatch.StartNew();
                var response = await _documentClientAccessor.DocumentClient.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(
                        _documentClientAccessor.RepositoryOptions.Database,
                        _documentClientAccessor.RepositoryOptions.Collection,
                        docToReplace.id),
                    docToReplace);
                watch.Stop();
                if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                {
                    TrackQuery(
                        now,
                        watch.Elapsed,
                        response.RequestCharge,
                        "ReplaceDocumentAsync",
                        _telemetryClient,
                        response.ContentLocation,
                        null,
                        null,
                        response.ActivityId,
                        response.RequestDiagnosticsString,
                        isContinuation: false,
                        resultCount: null);
                }

                return response;
            }
        }

        /// <inheritdoc/>
        public async Task<PlatformResponse<TResult>> ExecuteNextAsync<T, TResult>(StringWriter queryWriter, SqlParameterCollection sqlParameterCollection, string documentAlias, string orderBy = null, FeedOptions feedOptions = null, string tenantWhereClausePlaceHolderRef = null, IPolicyContext policyContext = null)
        {
            SqlQuerySpec querySpec = null;
            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy)
            {
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is true");
                EntityPolicyAttributes policyAttribtes = new EntityPolicyAttributes()
                {
                    IsCacheable = false,
                    IsEntityMultiTenant = true,
                }; // default settings for any document/table.
                var docType = _policyHelper.GetDocType(sqlParameterCollection);
                _logger.LogDebug("Persistence:docType = {docType}", LoggerHelper.SanitizeValue(docType));
                if (policyContext == null || (string.IsNullOrEmpty(policyContext.PolicyDocName) && string.IsNullOrEmpty(policyContext.PolicyDoc)))
                {
                    if (!string.IsNullOrEmpty(docType))
                    {
                        // read policy doc
                        var policyCtx = new PolicyContext(_userContextService) { PolicyDocName = docType + PolicyHelperMgr.PolicyDocumentSuffix };
                        policyAttribtes = await _policyHelper.ReadEntityPolicyAttributes(policyCtx).ConfigureAwait(false);
                    }
                }
                else
                {
                    policyAttribtes = await _policyHelper.ReadEntityPolicyAttributes(policyContext).ConfigureAwait(false);
                }

                _logger.LogDebug("Persistence:policyAttribtes: IsEntityMultiTenant = {IsEntityMultiTenant}, IsCacheable = {IsCacheable}", LoggerHelper.SanitizeValue(policyAttribtes.IsEntityMultiTenant), LoggerHelper.SanitizeValue(policyAttribtes.IsCacheable));

                // Form the query.
                querySpec = GetQuerySpec(
                    queryWriter,
                    sqlParameterCollection,
                    documentAlias,
                    policyAttribtes,
                    orderBy,
                    tenantWhereClausePlaceHolderRef);

                if (policyAttribtes.IsCacheable)
                {
                    return await ExecuteNextAsyncApplyCacheImpl<T, TResult>(querySpec, sqlParameterCollection, _documentClientAccessor.RepositoryOptions, feedOptions).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is false");

                // get Query.
                querySpec = GetQuerySpec(
                    queryWriter,
                    sqlParameterCollection,
                    documentAlias,
                    new EntityPolicyAttributes() { IsCacheable = false, IsEntityMultiTenant = false },
                    orderBy,
                    tenantWhereClausePlaceHolderRef);
            }

            // execute default
            return await ExecuteNextImplAsync<T, TResult>(querySpec, feedOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PlatformResponse<TResult>> ExecuteNextAsyncForSearches<T, TResult>(StringWriter queryWriter, SqlParameterCollection sqlParameterCollection, string documentAlias, string orderBy = null, FeedOptions feedOptions = null, string tenantWhereClausePlaceHolderRef = null, IPolicyContext policyContext = null)
        {
            SqlQuerySpec querySpec = null;
            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy)
            {
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is true");
                EntityPolicyAttributes policyAttribtes = new EntityPolicyAttributes()
                {
                    IsCacheable = false,
                    IsEntityMultiTenant = true,
                }; // default settings for any document/table.
                var docType = _policyHelper.GetDocType(sqlParameterCollection);
                _logger.LogDebug("Persistence:docType = {docType}", LoggerHelper.SanitizeValue(docType));

                // default.
                if (policyContext == null || (string.IsNullOrEmpty(policyContext.PolicyDocName) && string.IsNullOrEmpty(policyContext.PolicyDoc)))
                {
                    // read policy doc
                    if (!string.IsNullOrEmpty(docType))
                    {
                        var policyCtx = new PolicyContext(_userContextService) { PolicyDocName = docType + PolicyHelperMgr.PolicyDocumentSuffix };
                        policyAttribtes = await _policyHelper.ReadEntityPolicyAttributes(policyCtx).ConfigureAwait(false);
                    }
                }
                else
                {
                    policyAttribtes = await _policyHelper.ReadEntityPolicyAttributes(policyContext).ConfigureAwait(false);
                }

                _logger.LogDebug("Persistence:policyAttribtes: IsEntityMultiTenant= {IsEntityMultiTenant}, IsCacheable = {IsCacheable}", LoggerHelper.SanitizeValue(policyAttribtes.IsEntityMultiTenant), LoggerHelper.SanitizeValue(policyAttribtes.IsCacheable));

                // Form the query.
                querySpec = GetQuerySpecForSearches(
                    queryWriter,
                    sqlParameterCollection,
                    documentAlias,
                    policyAttribtes,
                    orderBy,
                    tenantWhereClausePlaceHolderRef);

                if (policyAttribtes.IsCacheable)
                {
                    return await ExecuteNextAsyncApplyCacheImpl<T, TResult>(querySpec, sqlParameterCollection, _documentClientAccessor.RepositoryOptions, feedOptions).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is false");
                querySpec = GetQuerySpecForSearches(
                    queryWriter,
                    sqlParameterCollection,
                    documentAlias,
                    new EntityPolicyAttributes() { IsCacheable = false, IsEntityMultiTenant = false },
                    orderBy,
                    tenantWhereClausePlaceHolderRef);
            }

            return await ExecuteNextImplAsync<T, TResult>(querySpec, feedOptions).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<PlatformResponse<TResult>> ExecuteNextAsync<T, TResult>(StringWriter queryWriter, SqlParameterCollection sqlParameterCollection, FeedOptions feedOptions = null)
        {
            return ExecuteNextNativeAsync<T, TResult>(queryWriter, sqlParameterCollection, feedOptions);
        }

        /// <inheritdoc/>
        public Task<PlatformResponse<TResult>> ExecuteNextNativeAsync<T, TResult>(StringWriter queryWriter, SqlParameterCollection sqlParameterCollection, FeedOptions feedOptions = null)
        {
            if (queryWriter == null)
            {
                throw new ArgumentNullException(nameof(queryWriter));
            }

            if (feedOptions == null)
            {
                feedOptions = new FeedOptions();
            }

            UpdateFeedOptions(feedOptions);
            var querySpec = new SqlQuerySpec { QueryText = queryWriter.ToString(), Parameters = sqlParameterCollection };
            return ExecuteNextImplAsync<T, TResult>(querySpec, feedOptions);
        }

        /// <inheritdoc/>
        public Task<DocumentResponse<T>> ReadDocumentAsync<T>(string id, RequestOptions requestOptions)
        {
            return _documentClientAccessor.DocumentClient.ReadDocumentAsync<T>(UriFactory.CreateDocumentUri(_documentClientAccessor.RepositoryOptions.Database, _documentClientAccessor.RepositoryOptions.Collection, id), requestOptions);
        }

        /// <inheritdoc/>
        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcId, List<BaseModel> allDocuments, RequestOptions requestOptions = null, params dynamic[] procedureParams)
        {
            return ExecuteStoredProcedureAsync<TValue>(storedProcId, _policyContext, allDocuments, requestOptions, procedureParams);
        }

        /// <inheritdoc/>
        public async Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcId, IPolicyContext policyContext, List<BaseModel> allDocuments, RequestOptions requestOptions = null, params dynamic[] procedureParams)
        {
            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy)
            {
                if (allDocuments != null && allDocuments.Count > 0)
                {
                    // update audit and tenant info in parallel.
                    List<Task<object>> tasks = new List<Task<object>>();
                    foreach (var entity in allDocuments)
                    {
                        tasks.Add(UpdateAuditAndTenantInfo(entity, policyContext));
                    }

                    var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            var response = await _documentClientAccessor.DocumentClient.ExecuteStoredProcedureAsync<TValue>(
                UriFactory.CreateStoredProcedureUri(
                    _documentClientAccessor.RepositoryOptions.Database,
                    _documentClientAccessor.RepositoryOptions.Collection,
                    storedProcId),
                requestOptions,
                procedureParams).ConfigureAwait(false);
            watch.Stop();
            if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
            {
                TrackQuery(
                    now,
                    watch.Elapsed,
                    response.RequestCharge,
                    "ExecuteStoredProcedureAsync",
                    _telemetryClient,
                    null,
                    null,
                    null,
                    response.ActivityId,
                    response.RequestDiagnosticsString,
                    isContinuation: false,
                    resultCount: null);
            }

            return response;
        }

        /// <summary>
        /// Track query execution in Application Insights.
        /// </summary>
        /// <param name="start">Start execution time.</param>
        /// <param name="duration">Duration.</param>
        /// <param name="requestCharge">Request charge measured in request units.</param>
        /// <param name="kind">Kind of Query.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="contentLocation">Content Location.</param>
        /// <param name="queryMetrics">Query Metrics.</param>
        /// <param name="sqlQuerySpec">SQL Query.</param>
        /// <param name="activityId">Activity Id.</param>
        /// <param name="requestDiagnosticsString">Diagnostics information for the current request to Azure Cosmos DB service.</param>
        /// <param name="isContinuation"><c>true</c> if the request is a continuation; otherwise, <c>false</c>.</param>
        /// <param name="resultCount">Number of documents returned.</param>
        private void TrackQuery(
            DateTimeOffset start,
            TimeSpan duration,
            double requestCharge,
            string kind,
            TelemetryClient telemetryClient,
            string contentLocation,
            IReadOnlyDictionary<string, QueryMetrics> queryMetrics,
            SqlQuerySpec sqlQuerySpec,
            string activityId,
            string requestDiagnosticsString,
            bool isContinuation,
            int? resultCount)
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            var dependency = new DependencyTelemetry(
                    "COSMOSDB",
                    contentLocation ?? string.Empty,
                    "COSMOSDB",
                    requestCharge.ToString(CultureInfo.InvariantCulture),
                    start,
                    duration,
                    "0", // Result code : we can't capture 429 here anyway
                    true); // We assume this call is successful, otherwise an exception would be thrown before.

            dependency.Metrics["Query.Charge"] = requestCharge;
            dependency.Properties["Query.Kind"] = kind;
            dependency.Properties["Query.Metrics"] = queryMetrics != null ? JsonConvert.SerializeObject(queryMetrics) : string.Empty;
            dependency.Properties["Query.Spec"] = sqlQuerySpec != null ? JsonConvert.SerializeObject(sqlQuerySpec) : string.Empty;
            dependency.Properties["Query.IsContinuation"] = isContinuation.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
            dependency.Properties["Query.ResultCount"] = resultCount?.ToInvariantString();
            dependency.Properties["ActivityId"] = activityId ?? string.Empty;

            if (_documentClientAccessor.RepositoryOptions.CaptureRequestDiagnostics)
            {
                dependency.Properties["Query.DiagnosticsString"] = requestDiagnosticsString ?? string.Empty;
            }

            telemetryClient.TrackDependency(dependency);
        }

        /// <summary>
        ///  Create a SQL query to use in Azure Cosmos DB service.
        /// </summary>
        /// <param name="queryWriter">StringWriter.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="alias">Alias.</param>
        /// <param name="entityPolicyAttributes">EntityPolicyAttributes.</param>
        /// <param name="orderBy">Order By condition.</param>
        /// <param name="tenantWhereClausePlaceHolderRef">Place holder to set the tenancy condition.</param>
        /// <returns> SQL query.</returns>
        /// <remarks>Internal for testing purposes.</remarks>
        internal SqlQuerySpec GetQuerySpec(
            StringWriter queryWriter,
            SqlParameterCollection sqlParameterCollection,
            string alias,
            EntityPolicyAttributes entityPolicyAttributes,
            string orderBy = null,
            string tenantWhereClausePlaceHolderRef = null)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias), "Alias for document is required");
            }

            if (entityPolicyAttributes == null)
            {
                throw new ArgumentNullException(nameof(entityPolicyAttributes));
            }

            if (queryWriter == null)
            {
                throw new ArgumentNullException(nameof(queryWriter));
            }

            // NOTE: StringWriter is memory-bound and should not use async/await

            SqlQuerySpec querySpec = null;
            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy && entityPolicyAttributes.IsEntityMultiTenant)
            {
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is true AND entityPolicyAttributes.IsEntityMultiTenant is true");
                string whereClause = _policyHelper.GetCosmosWhereClause(alias, sqlParameterCollection, entityPolicyAttributes, _policyContext);
                _logger.LogDebug("Persistence:whereClause = {whereClause}", LoggerHelper.SanitizeValue(whereClause));
                if (string.IsNullOrEmpty(tenantWhereClausePlaceHolderRef))
                {
                    queryWriter.WriteLine(whereClause);
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec { QueryText = queryWriter.ToString(), Parameters = sqlParameterCollection };
                }
                else
                {
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec
                    {
                        QueryText = queryWriter.ToString().Replace(tenantWhereClausePlaceHolderRef, whereClause),
                        Parameters = sqlParameterCollection,
                    };
                }
            }
            else
            {
                // just return the original query.
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is false OR entityPolicyAttributes.IsEntityMultiTenant is false");
                if (!string.IsNullOrEmpty(tenantWhereClausePlaceHolderRef))
                {
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec
                    {
                        QueryText = queryWriter.ToString().Replace(tenantWhereClausePlaceHolderRef, " " + string.Empty + " "),
                        Parameters = sqlParameterCollection,
                    };
                }
                else
                {
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec
                    {
                        QueryText = queryWriter.ToString(),
                        Parameters = sqlParameterCollection,
                    };
                }
            }

            return querySpec;
        }

        /// <summary>
        ///  Create a SQL query to use in Azure Cosmos DB service.
        /// </summary>
        /// <param name="queryWriter">StringWriter.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="alias">Alias.</param>
        /// <param name="entityPolicyAttributes">EntityPolicyAttributes.</param>
        /// <param name="orderBy">Order By condition.</param>
        /// <param name="tenantWhereClausePlaceHolderRef">Place holder to set the tenancy condition.</param>
        /// <returns> SQL query.</returns>
        /// <remarks>Internal for testing purposes.</remarks>
        internal SqlQuerySpec GetQuerySpecForSearches(
            StringWriter queryWriter,
            SqlParameterCollection sqlParameterCollection,
            string alias,
            EntityPolicyAttributes entityPolicyAttributes,
            string orderBy = null,
            string tenantWhereClausePlaceHolderRef = null)
        {
            if (queryWriter == null)
            {
                throw new ArgumentNullException(nameof(queryWriter));
            }

            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias), "Alias for document is required");
            }

            if (entityPolicyAttributes == null)
            {
                throw new ArgumentNullException(nameof(entityPolicyAttributes));
            }

            // NOTE: StringWriter is memory-bound and should not use async/await

            SqlQuerySpec querySpec = null;
            if (_documentClientAccessor.RepositoryOptions.ApplyDocumentPolicy && entityPolicyAttributes.IsEntityMultiTenant)
            {
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is true AND entityPolicyAttributes.IsEntityMultiTenant is true");
                string whereClause = _policyHelper.GetCosmosWhereClauseForSearches(alias, sqlParameterCollection, entityPolicyAttributes, _policyContext);
                _logger.LogDebug($"Persistence:whereClause = {LoggerHelper.SanitizeValue(whereClause)}");
                if (string.IsNullOrEmpty(tenantWhereClausePlaceHolderRef))
                {
                    queryWriter.WriteLine(whereClause);
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec { QueryText = queryWriter.ToString(), Parameters = sqlParameterCollection };
                }
                else
                {
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec { QueryText = queryWriter.ToString().Replace(tenantWhereClausePlaceHolderRef, whereClause), Parameters = sqlParameterCollection };
                }
            }
            else
            {
                // just return the original query.
                _logger.LogDebug("Persistence:options.ApplyDocumentPolicy is false OR entityPolicyAttributes.IsEntityMultiTenant is false");
                if (!string.IsNullOrEmpty(tenantWhereClausePlaceHolderRef))
                {
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec { QueryText = queryWriter.ToString().Replace(tenantWhereClausePlaceHolderRef, " " + string.Empty + " "), Parameters = sqlParameterCollection };
                }
                else
                {
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        queryWriter.WriteLine(" " + orderBy + " ");
                    }

                    querySpec = new SqlQuerySpec { QueryText = queryWriter.ToString(), Parameters = sqlParameterCollection };
                }
            }

            return querySpec;
        }

        /// <summary>
        /// Update FeedOptions.
        /// </summary>
        /// <param name="feedOptions">FeedOptions.</param>
        private void UpdateFeedOptions(FeedOptions feedOptions)
        {
            if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
            {
                feedOptions.PopulateQueryMetrics = true;
            }

            if (_documentClientAccessor.RepositoryOptions.CaptureIndexMetrics)
            {
                feedOptions.PopulateIndexMetrics = true;
            }
        }

        /// <summary>
        /// Executes the query and retrieves the next page of results from the Cache,
        /// if not exists in cache from the Azure Cosmos DB service.
        /// </summary>
        /// <typeparam name="T">Type of document to retrieve.</typeparam>
        /// <typeparam name="TResult">Result of query.</typeparam>
        /// <param name="querySpec">SQL Query.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="repositoryOptions">RepositoryOptions.</param>
        /// <param name="feedOptions">FeedOptions.</param>
        /// <returns>PlatformResponse object with the results of the query.</returns>
        private async Task<PlatformResponse<TResult>> ExecuteNextAsyncApplyCacheImpl<T, TResult>(SqlQuerySpec querySpec, SqlParameterCollection sqlParameterCollection, IRepositoryOptions repositoryOptions, FeedOptions feedOptions = null)
        {
            var docType = _policyHelper.GetDocType(sqlParameterCollection);
            bool callToSrcSuccess = false;
            PlatformResponse<TResult> platformResponse = null;
            if (querySpec != null)
            {
                string keyHash = Md5Hash.GetHash(JsonConvert.SerializeObject(querySpec));
                _logger.LogDebug("Persistence:keyHash  = {keyHash}", LoggerHelper.SanitizeValue(keyHash));
                if (_distributedCache != null && !string.IsNullOrEmpty(keyHash))
                {
                    _logger.LogDebug("Persistence:distributedCache is not null AND Key hash is not null");
                    try
                    {
                        string cacheKey = (!string.IsNullOrEmpty(docType) ? docType : "defaultCacheKey") + "_" + keyHash;
                        _logger.LogDebug("Persistence:cacheKey = {cacheKey}", LoggerHelper.SanitizeValue(cacheKey));

                        var now = DateTimeOffset.UtcNow;
                        var watch = Stopwatch.StartNew();
                        var data = await _distributedCache.GetStringAsync(cacheKey).ConfigureAwait(false);
                        watch.Stop();
                        var retrieveTime = watch.ElapsedMilliseconds;
                        _logger.LogDebug($"Persistence:ExecuteNextAsyncApplyCacheImpl, time (ms) to retrieve from cache = {LoggerHelper.SanitizeValue(retrieveTime)}.");
                        if (!string.IsNullOrEmpty(data))
                        {
                            _logger.LogDebug("Persistence:retrieved data from cache");
                            return JsonConvert.DeserializeObject<PlatformResponse<TResult>>(data);
                        }
                        else
                        {
                            _logger.LogDebug("Persistence:data not found in cache, retrieving data from source");
                            platformResponse = await ExecuteNextImplAsync<T, TResult>(querySpec, feedOptions).ConfigureAwait(false);
                            callToSrcSuccess = true;
                            if (platformResponse != null && platformResponse.List != null && platformResponse.List.Count > 0)
                            {
                                _logger.LogDebug("Persistence:setting data into cache from source");

                                var expirationMinutes = repositoryOptions.PolicyTenancyExpirationInMinutes > 0 ? repositoryOptions.PolicyTenancyExpirationInMinutes : 120;
                                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(0, expirationMinutes, 0) };
                                await _distributedCache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(platformResponse), options).ConfigureAwait(false);
                            }

                            return platformResponse;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Persistence:Error occurred retrieving data from cache. {Message}", e?.Message);
                    }
                }
            }

            if (callToSrcSuccess)
            {
                return platformResponse;
            }
            else
            {
                return await ExecuteNextImplAsync<T, TResult>(querySpec, feedOptions).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the query and retrieves the next page of results in the Azure Cosmos
        /// DB service.
        /// </summary>
        /// <typeparam name="T">Type of document to retrieve.</typeparam>
        /// <typeparam name="TResult">Result of query.</typeparam>
        /// <param name="querySpec">SQL Query.</param>
        /// <param name="feedOptions">FeedOptions.</param>
        /// <returns>PlatformResponse object with the results of the query.</returns>
        private async Task<PlatformResponse<TResult>> ExecuteNextImplAsync<T, TResult>(SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            if (feedOptions == null)
            {
                feedOptions = new FeedOptions();
            }

            UpdateFeedOptions(feedOptions);
            PlatformResponse<TResult> platformResponse = new PlatformResponse<TResult>()
            {
                List = new List<TResult>(),
            };

            // execute query.
            var queryable = _documentClientAccessor.DocumentClient.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(
                    _documentClientAccessor.RepositoryOptions.Database,
                    _documentClientAccessor.RepositoryOptions.Collection),
                querySpec,
                feedOptions).AsDocumentQuery();
            var fetchedRecordCount = 0;
            var isFirstIteration = true;
            FeedResponse<TResult> response = null;
            string continuationToken = string.Empty;

            if (feedOptions.MaxItemCount == null || !feedOptions.MaxItemCount.HasValue)
            {
                feedOptions.MaxItemCount = 100;
            }

            // I know, its very conservative!, just doing it so that there is no null reference error here.
            if (feedOptions != null && feedOptions.MaxItemCount != null && feedOptions.MaxItemCount.HasValue
                && (feedOptions.MaxItemCount.Value > 0 || feedOptions.MaxItemCount.Value == -1))
            {
                List<FeedResponse<TResult>> result = new List<FeedResponse<TResult>>();
                do
                {
                    if (isFirstIteration)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var watch = Stopwatch.StartNew();
                        response = await queryable.ExecuteNextAsync<TResult>().ConfigureAwait(false);
                        watch.Stop();
                        continuationToken = response.ResponseContinuation;
                        isFirstIteration = false;
                        if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                        {
                            var elapsedTime = watch.ElapsedMilliseconds;
                            _logger.LogDebug($"Persistence: time elapsed to execute the query = {LoggerHelper.SanitizeValue(elapsedTime)}, query = {LoggerHelper.SanitizeValue(querySpec)} ");
                            TrackQuery(
                                now,
                                watch.Elapsed,
                                response.RequestCharge,
                                "ExecuteNextAsync",
                                _telemetryClient,
                                response.ContentLocation,
                                response.QueryMetrics,
                                querySpec,
                                response.ActivityId,
                                response.RequestDiagnosticsString,
                                isContinuation: false,
                                resultCount: response?.Count);
                        }
                    }
                    else
                    {
                        var feedOptionsCopy = ReflectionHelper.Map<FeedOptions, FeedOptions>(feedOptions);
                        feedOptionsCopy.MaxItemCount = feedOptions.MaxItemCount - fetchedRecordCount; // in this iteration, only fetching the remaining ones.
                        feedOptionsCopy.PartitionKey = feedOptions.PartitionKey;
                        feedOptionsCopy.RequestContinuation = continuationToken;
                        var now = DateTimeOffset.UtcNow;
                        var watch = Stopwatch.StartNew();
                        queryable = _documentClientAccessor.DocumentClient.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(_documentClientAccessor.RepositoryOptions.Database, _documentClientAccessor.RepositoryOptions.Collection), querySpec, feedOptionsCopy).AsDocumentQuery();
                        response = await queryable.ExecuteNextAsync<TResult>().ConfigureAwait(false);
                        watch.Stop();

                        if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                        {
                            var elapsedTime = watch.ElapsedMilliseconds;
                            _logger.LogDebug($"Persistence:HasMoreResults time elapsed to execute the query= {LoggerHelper.SanitizeValue(elapsedTime)}, query = {LoggerHelper.SanitizeValue(querySpec)} ");
                            TrackQuery(
                                now,
                                watch.Elapsed,
                                response.RequestCharge,
                                "ExecuteNextAsync",
                                _telemetryClient,
                                response.ContentLocation,
                                response.QueryMetrics,
                                querySpec,
                                response.ActivityId,
                                response.RequestDiagnosticsString,
                                isContinuation: true,
                                resultCount: response?.Count);
                        }

                        continuationToken = response.ResponseContinuation;
                    }

                    if (response != null && response.Count > 0)
                    {
                        result.Add(response);
                        fetchedRecordCount = fetchedRecordCount + response.Count;
                    }
                }
                while (queryable.HasMoreResults && (fetchedRecordCount < feedOptions.MaxItemCount || feedOptions.MaxItemCount == -1));

                platformResponse = new PlatformResponse<TResult>() { List = new List<TResult>(result.SelectMany(d => d)), ResponseContinuation = continuationToken };

                return platformResponse;
            }
            else
            {
                var now = DateTimeOffset.UtcNow;
                var watch = Stopwatch.StartNew();
                response = await queryable.ExecuteNextAsync<TResult>().ConfigureAwait(false);
                watch.Stop();
                platformResponse = new PlatformResponse<TResult>() { ResponseContinuation = response?.ResponseContinuation };
                var list = new List<TResult>();
                list.AddRange(response);
                platformResponse.List = list;
                if (_documentClientAccessor.RepositoryOptions.CaptureQueryMetrics)
                {
                    var timeElapsed = watch.ElapsedMilliseconds;
                    _logger.LogDebug("Persistence: time elapsed to execute the query= {timeElapsed}, query = {query} ", LoggerHelper.SanitizeValue(timeElapsed), LoggerHelper.SanitizeValue(querySpec));
                    TrackQuery(
                        now,
                        watch.Elapsed,
                        response.RequestCharge,
                        "ExecuteNextAsync",
                        _telemetryClient,
                        response.ContentLocation,
                        response.QueryMetrics,
                        querySpec,
                        response.ActivityId,
                        response.RequestDiagnosticsString,
                        isContinuation: false,
                        resultCount: response?.Count);
                }
            }

            return platformResponse;
        }

        /// <summary>
        /// Update Audit Fields and Tenant Fields.
        /// </summary>
        /// <param name="baseModel">Document to update, must implementing BaseModel class.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Document with updated audit and tenancy fields.</returns>
        private Task<object> UpdateAuditAndTenantInfo(BaseModel baseModel, IPolicyContext policyContext)
        {
            // update audit
            UpsertAuditFields(baseModel);

            // Apply Tenancy
            return _policyHelper.SetTenantIdsForInsert(baseModel, policyContext);
        }

        /// <summary>
        /// Update Audit Fields.
        /// </summary>
        /// <param name="baseModel">Document to update, must implementing BaseModel class.</param>
        private void UpsertAuditFields(BaseModel baseModel)
        {
            if (baseModel.Audit == null)
            {
                baseModel.Audit = new Exos.Platform.TenancyHelper.Models.AuditModel()
                {
                    CreatedBy = _userContextService.UserId,
                    CreatedDate = DateTimeOffset.UtcNow,
                    IsDeleted = false,
                    LastUpdatedBy = _userContextService.UserId,
                    LastUpdatedDate = DateTimeOffset.UtcNow,
                };
            }
            else
            {
                if (string.IsNullOrEmpty(baseModel.Audit.CreatedBy))
                {
                    baseModel.Audit.CreatedBy = _userContextService.UserId;
                }

                if (baseModel.Audit.CreatedDate == DateTime.MinValue)
                {
                    baseModel.Audit.CreatedDate = DateTimeOffset.UtcNow;
                }

                if (string.IsNullOrEmpty(baseModel.Audit.LastUpdatedBy))
                {
                    baseModel.Audit.LastUpdatedBy = _userContextService.UserId;
                }

                if (baseModel.Audit.LastUpdatedDate == DateTime.MinValue)
                {
                    baseModel.Audit.LastUpdatedDate = DateTimeOffset.UtcNow;
                }
            }
        }

        /// <summary>
        /// Create audit fields.
        /// </summary>
        /// <param name="baseModel">Document to create, must implementing BaseModel class.</param>
        private void CreateAuditFields(BaseModel baseModel)
        {
            baseModel.Audit = new Exos.Platform.TenancyHelper.Models.AuditModel()
            {
                CreatedBy = _userContextService.UserId,
                CreatedDate = DateTimeOffset.UtcNow,
                IsDeleted = false,
                LastUpdatedBy = _userContextService.UserId,
                LastUpdatedDate = DateTimeOffset.UtcNow,
            };
        }
    }

    /// <inheritdoc/>
    public class PersistenceServiceInstance1 : PersistenceService, IPersistenceServiceInstance1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceServiceInstance1"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">IDocumentClientAccessor.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceServiceInstance1(
            IDocumentClientAccessor1 documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelperInstance1 policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceServiceInstance1> logger,
            TelemetryClient telemetryClient) : base(documentClientAccessor, distributedCache, userContextService, policyHelper, policyContext, logger, telemetryClient)
        {
        }
    }

    /// <inheritdoc/>
    public class PersistenceServiceInstance2 : PersistenceService, IPersistenceServiceInstance2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceServiceInstance2"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">IDocumentClientAccessor.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceServiceInstance2(
            IDocumentClientAccessor2 documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelperInstance2 policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceServiceInstance2> logger,
            TelemetryClient telemetryClient) : base(documentClientAccessor, distributedCache, userContextService, policyHelper, policyContext, logger, telemetryClient)
        {
        }
    }

    /// <inheritdoc/>
    public class PersistenceServiceInstance3 : PersistenceService, IPersistenceServiceInstance3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceServiceInstance3"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">IDocumentClientAccessor.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceServiceInstance3(
            IDocumentClientAccessor3 documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelperInstance3 policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceServiceInstance3> logger,
            TelemetryClient telemetryClient) : base(documentClientAccessor, distributedCache, userContextService, policyHelper, policyContext, logger, telemetryClient)
        {
        }
    }

    /// <inheritdoc/>
    public class PersistenceServiceInstance4 : PersistenceService, IPersistenceServiceInstance4
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceServiceInstance4"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">IDocumentClientAccessor.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceServiceInstance4(
            IDocumentClientAccessor4 documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelperInstance4 policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceServiceInstance4> logger,
            TelemetryClient telemetryClient) : base(documentClientAccessor, distributedCache, userContextService, policyHelper, policyContext, logger, telemetryClient)
        {
        }
    }

    /// <inheritdoc/>
    public class PersistenceServiceInstance5 : PersistenceService, IPersistenceServiceInstance5
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceServiceInstance5"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">IDocumentClientAccessor.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceServiceInstance5(
            IDocumentClientAccessor5 documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelperInstance5 policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceServiceInstance5> logger,
            TelemetryClient telemetryClient) : base(documentClientAccessor, distributedCache, userContextService, policyHelper, policyContext, logger, telemetryClient)
        {
        }
    }

    /// <inheritdoc/>
    public class PersistenceServiceInstance6 : PersistenceService, IPersistenceServiceInstance6
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceServiceInstance6"/> class.
        /// </summary>
        /// <param name="documentClientAccessor">IDocumentClientAccessor.</param>
        /// <param name="distributedCache">IDistributedCache.</param>
        /// <param name="userContextService">IUserContextService.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public PersistenceServiceInstance6(
            IDocumentClientAccessor6 documentClientAccessor,
            IDistributedCache distributedCache,
            IUserContextService userContextService,
            IPolicyHelperInstance6 policyHelper,
            IPolicyContext policyContext,
            ILogger<PersistenceServiceInstance6> logger,
            TelemetryClient telemetryClient) : base(documentClientAccessor, distributedCache, userContextService, policyHelper, policyContext, logger, telemetryClient)
        {
        }
    }
}
