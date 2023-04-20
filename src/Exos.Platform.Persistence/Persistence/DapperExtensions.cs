#pragma warning disable CA1062 // Validate arguments of public methods

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Resiliency.Policies;
using Exos.Platform.Persistence.Encryption;
using Exos.Platform.TenancyHelper.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using static Dapper.SqlMapper;

namespace Exos.Platform.Persistence
{
    /// <summary>
    /// Options for query optimizer hints.
    /// </summary>
    public enum QueryOption
    {
        /// <summary>
        /// No optimize hint.
        /// </summary>
        None,

        /// <summary>
        /// Optimize hint OPTIMIZE FOR UNKNOWN.
        /// </summary>
        OptimizeForUnknown,

        /// <summary>
        /// Optimize hint RECOMPILE.
        /// </summary>
        Recompile,
    }

    /// <summary>
    /// Options to set the tenancy query in a specific position in the query.
    /// </summary>
    public enum TenancyPlaceHolder
    {
        /// <summary>
        /// To not use the tenancy place holder, tenancy condition will be added at the end of the query.
        /// </summary>
        None,

        /// <summary>
        /// Use tenancy place holder, tenancy query will be added in the first string parameter {0}.
        /// </summary>
        UseTenancyPlaceHolder,
    }

    /// <summary>
    /// Extension methods for Dapper queries, all the methods will have Multi-Tenancy where condition.
    /// </summary>
    public static class DapperExtensions
    {
        /// <summary>
        /// Optimize hint OPTIMIZE FOR UNKNOWN.
        /// </summary>
        public static readonly string QUERYHINTOPTIMIZEUNKNOWN = " OPTION (OPTIMIZE FOR UNKNOWN)";

        /// <summary>
        /// Optimize hint RECOMPILE.
        /// </summary>
        public static readonly string QUERYHINTRECOMPILE = "OPTION (RECOMPILE)";

        private static AsyncPolicy _sqlPolicy;
        private static int _commandTimeout;

        /// <summary>
        /// Track the query execution in ApplicationInsights.
        /// </summary>
        /// <param name="start">Query starting time.</param>
        /// <param name="duration">Duration of query execution.</param>
        /// <param name="sql">SQL Query.</param>
        /// <param name="kind">Kind of Query (usually method that calls the query).</param>
        /// <param name="tc">Telemetry Client.</param>
        /// <param name="param">Query parameters.</param>
        public static void TrackQuery(DateTimeOffset start, TimeSpan duration, string sql, string kind, TelemetryClient tc, object param = null)
        {
            if (tc != null)
            {
                var dependency = new DependencyTelemetry(
                    "SQLQuery",
                    string.Empty,
                    "SQLQuery",
                    sql,
                    start,
                    duration,
                    "0", // Result code : we can't capture 429 here anyway
                    true); // We assume this call is successful, otherwise an exception would be thrown before.

                dependency.Metrics["Duration"] = duration.TotalMilliseconds;
                dependency.Properties["Kind"] = kind;
                dependency.Properties["Duration"] = duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                dependency.Properties["QueryText"] = sql ?? string.Empty;

                if (param is IDynamicParameters)
                {
                    var dynamicParams = (DynamicParameters)param;
                    var parameters = dynamicParams.ParameterNames
                        .ToList().Select(p => new { ParameterName = p, ParameterValue = ((IParameterLookup)dynamicParams)[p] })
                        .ToList();
                    dependency.Properties["Queryparameters"] = param != null ? JsonSerializer.Serialize(parameters) : string.Empty;
                }
                else
                {
                    dependency.Properties["Queryparameters"] = param != null ? JsonSerializer.Serialize(param) : string.Empty;
                }

                tc.TrackDependency(dependency);
            }
        }

        /// <summary>
        /// Override extension method QueryAsync adding Multi-Tenancy where condition.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of T.</returns>
        public static async Task<IEnumerable<T>> QueryAsyncTenancy<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryAsyncTenancyConnection<T>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias);

            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of T.</returns>
        public static async Task<IEnumerable<T>> QueryAsyncTenancyConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            IEnumerable<T> result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            if (result.Any() && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptQueryResults<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync adding Multi-Tenancy where condition, his adds ServiceId IN clause instead of ServiceId equals clause.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of T.</returns>
        public static async Task<IEnumerable<T>> SearchQueryAsyncTenancy<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.SearchQueryAsyncTenancyConnection<T>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync adding Multi-Tenancy where condition, his adds ServiceId IN clause instead of ServiceId equals clause.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of T.</returns>
        public static async Task<IEnumerable<T>> SearchQueryAsyncTenancyConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClauseForSearches(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            IEnumerable<T> result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            if (result.Any() && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptQueryResults<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "SearchQueryAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QuerySingleOrDefaultAsync adding Multi-Tenancy where condition.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QuerySingleOrDefaultAsyncTenancy<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QuerySingleOrDefaultAsyncTenancyConnection<T>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QuerySingleOrDefaultAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QuerySingleOrDefaultAsyncTenancyConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            T result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);

            if (result != null && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptResult<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QuerySingleOrDefaultAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryFirstAsync adding Multi-Tenancy where condition.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QueryFirstAsyncTenancy<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryFirstAsyncTenancyConnection<T>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryFirstAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QueryFirstAsyncTenancyConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            T result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType);
            if (result != null && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptResult<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryFirstAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryFirstOrDefaultAsync adding Multi-Tenancy where condition.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QueryFirstOrDefaultAsyncTenancy<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryFirstOrDefaultAsyncTenancyConnection<T>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryFirstOrDefaultAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QueryFirstOrDefaultAsyncTenancyConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            T result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);

            if (result != null && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptResult<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryFirstOrDefaultAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QuerySingleAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QuerySingleAsyncTenancy<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QuerySingleAsyncTenancyConnection<T>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QuerySingleAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QuerySingleAsyncTenancyConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            T result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType);

            if (result != null && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptResult<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QuerySingleAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync for 2 level Mapping
        /// adding Multi-Tenancy where condition.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mapFunction">The function to map row types to the return type.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of TReturn.</returns>
        public static async Task<IEnumerable<TReturn>> QueryAsyncTenancy<TFirst, TSecond, TReturn>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            Func<TFirst, TSecond, TReturn> mapFunction,
            string splitOn = "Id",
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryAsyncTenancyConnection<TFirst, TSecond, TReturn>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                mapFunction,
                splitOn,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync for 2 level Mapping
        /// adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mapFunction">The function to map row types to the return type.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of TReturn.</returns>
        public static async Task<IEnumerable<TReturn>> QueryAsyncTenancyConnection<TFirst, TSecond, TReturn>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            Func<TFirst, TSecond, TReturn> mapFunction,
            string splitOn = "Id",
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            IEnumerable<TReturn> result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryAsync<TFirst, TSecond, TReturn>(sql, mapFunction, param, transaction, true, splitOn, commandTimeout, commandType))
                :
                await dbconnection.QueryAsync<TFirst, TSecond, TReturn>(sql, mapFunction, param, transaction, true, splitOn, commandTimeout, commandType);

            if (result.Any() && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptQueryResults<TReturn>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync for 3 level Mapping
        /// adding Multi-tenancy where condition.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mapFunction">The function to map row types to the return type.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of TReturn.</returns>
        public static async Task<IEnumerable<TReturn>> QueryAsyncTenancy<TFirst, TSecond, TThird, TReturn>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            Func<TFirst, TSecond, TThird, TReturn> mapFunction,
            string splitOn = "Id",
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryAsyncTenancyConnection<TFirst, TSecond, TThird, TReturn>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                mapFunction,
                splitOn,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync for 3 level Mapping
        /// adding Multi-tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mapFunction">The function to map row types to the return type.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of TReturn.</returns>
        public static async Task<IEnumerable<TReturn>> QueryAsyncTenancyConnection<TFirst, TSecond, TThird, TReturn>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            Func<TFirst, TSecond, TThird, TReturn> mapFunction,
            string splitOn = "Id",
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            IEnumerable<TReturn> result = _sqlPolicy != null ?
                await dbconnection.QueryAsync<TFirst, TSecond, TThird, TReturn>(sql, mapFunction, param, transaction, true, splitOn, commandTimeout, commandType)
                :
                await dbconnection.QueryAsync<TFirst, TSecond, TThird, TReturn>(sql, mapFunction, param, transaction, true, splitOn, commandTimeout, commandType);

            if (result.Any() && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptQueryResults<TReturn>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync for 4 level Mapping
        /// adding Multi-tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mapFunction">The function to map row types to the return type.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of TReturn.</returns>
        public static async Task<IEnumerable<TReturn>> QueryAsyncTenancy<TFirst, TSecond, TThird, TFourth, TReturn>(
            this PlatformDbContext platformDbContext,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            Func<TFirst, TSecond, TThird, TFourth, TReturn> mapFunction,
            string splitOn = "Id",
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryAsyncTenancyConnection<TFirst, TSecond, TThird, TFourth, TReturn>(
                sql,
                tableAliases,
                policyHelper,
                logger,
                mapFunction,
                splitOn,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync for 4 level Mapping
        /// adding Multi-tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="dbconnection">The connection to query on.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="tableAliases">Table Alias.</param>
        /// <param name="policyHelper">Tenancy Policy Helper.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mapFunction">The function to map row types to the return type.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <param name="commandTimeout">The command timeout (in seconds).</param>
        /// <param name="commandType">The type of command to execute.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="tenancyPlaceHolder">Tenancy PlaceHolder.</param>
        /// <param name="queryOption">Query Option (optimizer hint).</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="workorderAlias">Workorder table Alias name.</param>
        /// <returns>An enumerable of TReturn.</returns>
        public static async Task<IEnumerable<TReturn>> QueryAsyncTenancyConnection<TFirst, TSecond, TThird, TFourth, TReturn>(
            this IDbConnection dbconnection,
            string sql,
            List<string> tableAliases,
            IPolicyHelper policyHelper,
            ILogger logger,
            Func<TFirst, TSecond, TThird, TFourth, TReturn> mapFunction,
            string splitOn = "Id",
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            var tenancyWhereClause = policyHelper.GetSQLWhereClause(tableAliases, null, workorderAlias);
            sql = AddTenancyWhereClause(sql, tenancyPlaceHolder, tenancyWhereClause);
            sql = ApplyQueryOptions(sql, queryOption);
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { tenancyQuery = sql })}.");
            var now = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            IEnumerable<TReturn> result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(sql, mapFunction, param, transaction, true, splitOn, commandTimeout, commandType))
                :
                await dbconnection.QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(sql, mapFunction, param, transaction, true, splitOn, commandTimeout, commandType);

            if (result.Any() && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptQueryResults<TReturn>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryAsyncTenancy", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync, return results decrypted.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        /// <returns>An enumerable of T.</returns>
        public static async Task<IEnumerable<T>> QueryAsync<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            IDatabaseEncryption databaseEncryption,
            bool validateKeyForDecryption = true,
            ILogger logger = null,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null)
        {
            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryAsyncConnection<T>(
                sql,
                databaseEncryption,
                validateKeyForDecryption,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryAsync, return results decrypted.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection"><see cref="IDbConnection"/>.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction"><see cref="IDbTransaction"/>.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        /// <returns>An enumerable of T.</returns>
        public static async Task<IEnumerable<T>> QueryAsyncConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            IDatabaseEncryption databaseEncryption,
            bool validateKeyForDecryption = true,
            ILogger logger = null,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null)
        {
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Stopwatch watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);
            IEnumerable<T> queryResult = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            if (queryResult.Any() && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                queryResult = DecryptQueryResults<T>(queryResult, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryAsync", telemetryClient, param);
            return queryResult;
        }

        /// <summary>
        /// Override extension method QueryFirstAsync, return results decrypted.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="platformDbContext">The PlatformDbContext.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QueryFirstAsync<T>(
            this PlatformDbContext platformDbContext,
            string sql,
            IDatabaseEncryption databaseEncryption,
            bool validateKeyForDecryption = true,
            ILogger logger = null,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null)
        {
            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryFirstAsyncConnection<T>(
                sql,
                databaseEncryption,
                validateKeyForDecryption,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryFirstAsync, return results decrypted.
        /// Using the IDbConnection.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="dbconnection"><see cref="IDbConnection"/>.</param>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction"><see cref="IDbTransaction"/>.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        /// <returns>A sequence of data of T.</returns>
        public static async Task<T> QueryFirstAsyncConnection<T>(
            this IDbConnection dbconnection,
            string sql,
            IDatabaseEncryption databaseEncryption,
            bool validateKeyForDecryption = true,
            ILogger logger = null,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null)
        {
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { query = sql })}.");
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Stopwatch watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);

            T result = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType);
            if (result != null && databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
                result = DecryptResult<T>(result, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, sql, "QueryFirstAsync", telemetryClient, param);
            return result;
        }

        /// <summary>
        /// Override extension method QueryMultipleAsync adding Multi-Tenancy where condition.
        /// </summary>
        /// <param name="platformDbContext">The platformDbContext<see cref="PlatformDbContext"/>.</param>
        /// <param name="dapperQueries">List of queries to execute, set a DapperQuery object for each query<see cref="List{DapperQuery}"/>.</param>
        /// <param name="policyHelper">Tenancy Policy Helper<see cref="IPolicyHelper"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.<see cref="object"/>.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient">The telemetryClient<see cref="TelemetryClient"/>.</param>
        /// <param name="tenancyPlaceHolder">The tenancyPlaceHolder<see cref="TenancyPlaceHolder"/>.</param>
        /// <param name="queryOption">Query Option (optimizer hint)<see cref="QueryOption"/>.</param>
        /// <param name="databaseEncryption">The databaseEncryption<see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.<see cref="bool"/>.</param>
        /// <param name="workorderAlias">Workorder table Alias name.<see cref="string"/>.</param>
        /// <returns>The result of each query, the result of each query is a collection.</returns>
        public static async Task<DapperQueryMultipleResults> QueryMultipleAsyncTenancy(
            this PlatformDbContext platformDbContext,
            IEnumerable<DapperQuery> dapperQueries,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryMultipleAsyncTenancyConnection(
                dapperQueries,
                policyHelper,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient,
                tenancyPlaceHolder,
                queryOption,
                databaseEncryption,
                validateKeyForDecryption,
                workorderAlias).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryMultipleAsync adding Multi-Tenancy where condition.
        /// Using the IDbConnection.
        /// </summary>
        /// <param name="dbconnection">The dbconnection<see cref="IDbConnection"/>.</param>
        /// <param name="dapperQueries">List of queries to execute, set a DapperQuery object for each query<see cref="List{DapperQuery}"/>.</param>
        /// <param name="policyHelper">Tenancy Policy Helper<see cref="IPolicyHelper"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.<see cref="object"/>.</param>
        /// <param name="transaction">The transaction<see cref="IDbTransaction"/>.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient">The telemetryClient<see cref="TelemetryClient"/>.</param>
        /// <param name="tenancyPlaceHolder">The tenancyPlaceHolder<see cref="TenancyPlaceHolder"/>.</param>
        /// <param name="queryOption">Query Option (optimizer hint)<see cref="QueryOption"/>.</param>
        /// <param name="databaseEncryption">The databaseEncryption<see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.<see cref="bool"/>.</param>
        /// <param name="workorderAlias">Workorder table Alias name.<see cref="string"/>.</param>
        /// <returns>The result of each query, the result of each query is a collection.</returns>
        public static async Task<DapperQueryMultipleResults> QueryMultipleAsyncTenancyConnection(
            this IDbConnection dbconnection,
            IEnumerable<DapperQuery> dapperQueries,
            IPolicyHelper policyHelper,
            ILogger logger,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null,
            TenancyPlaceHolder tenancyPlaceHolder = TenancyPlaceHolder.None,
            QueryOption queryOption = QueryOption.None,
            IDatabaseEncryption databaseEncryption = null,
            bool validateKeyForDecryption = true,
            string workorderAlias = null)
        {
            if (policyHelper == null)
            {
                throw new ArgumentNullException(nameof(policyHelper));
            }

            List<string> sqlQueries = new List<string>();
            List<Type> queryReturnTypes = new List<Type>();
            foreach (var dapperQuery in dapperQueries)
            {
                var tenancyWhereClause = policyHelper.GetSQLWhereClause(dapperQuery.TableAlias.ToList(), null, workorderAlias);
                dapperQuery.Query = AddTenancyWhereClause(dapperQuery.Query, tenancyPlaceHolder, tenancyWhereClause);
                dapperQuery.Query = ApplyQueryOptions(dapperQuery.Query, queryOption);
                sqlQueries.Add(dapperQuery.Query);
                queryReturnTypes.Add(dapperQuery.ReturnType);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            Stopwatch watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);

            var multipleSql = string.Join(";", sqlQueries.ToArray());
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { multipleQuery = multipleSql })}.");
            var multipleResult = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryMultipleAsync(multipleSql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryMultipleAsync(multipleSql, param, transaction, commandTimeout, commandType);

            if (databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
            }

            DapperQueryMultipleResults dapperQueryMultipleResults = new DapperQueryMultipleResults(queryReturnTypes);
            foreach (var returnType in queryReturnTypes)
            {
                var queryResult = await multipleResult.ReadAsync(returnType);
                queryResult = DecryptQueryResults<object>(queryResult, databaseEncryption);
                dapperQueryMultipleResults.AddQueryResult(queryResult);
            }

            if (databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, multipleSql, "QueryMultipleAsyncTenancy", telemetryClient, param);
            return dapperQueryMultipleResults;
        }

        /// <summary>
        /// Override extension method QueryMultipleAsync.
        /// </summary>
        /// <param name="platformDbContext">The platformDbContext<see cref="PlatformDbContext"/>.</param>
        /// <param name="dapperQueries">List of queries to execute, set a DapperQuery object for each query<see cref="List{DapperQuery}"/>.</param>
        /// <param name="databaseEncryption">The databaseEncryption<see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.<see cref="bool"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.<see cref="object"/>.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient">The telemetryClient<see cref="TelemetryClient"/>.</param>
        /// <returns>The result of each query, the result of each query is a collection.</returns>
        public static async Task<DapperQueryMultipleResults> QueryMultipleAsync(
            this PlatformDbContext platformDbContext,
            IEnumerable<DapperQuery> dapperQueries,
            IDatabaseEncryption databaseEncryption,
            bool validateKeyForDecryption = true,
            ILogger logger = null,
            object param = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null)
        {
            DbConnection dbconnection = platformDbContext.Database.GetDbConnection();
            DbTransaction dbtransaction = platformDbContext.Database.CurrentTransaction?.GetDbTransaction();

            var result = await dbconnection.QueryMultipleAsyncConnection(
                dapperQueries,
                databaseEncryption,
                validateKeyForDecryption,
                logger,
                param,
                dbtransaction,
                commandTimeout,
                commandType,
                telemetryClient).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Override extension method QueryMultipleAsync.
        /// Using the IDbConnection.
        /// </summary>
        /// <param name="dbconnection">The dbconnection<see cref="IDbConnection"/>.</param>
        /// <param name="dapperQueries">List of queries to execute, set a DapperQuery object for each query<see cref="List{DapperQuery}"/>.</param>
        /// <param name="databaseEncryption">The databaseEncryption<see cref="IDatabaseEncryption"/>.</param>
        /// <param name="validateKeyForDecryption">Sets the value indicating whether the request validates the key for decryption if is set to false the request can decrypt any record.<see cref="bool"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger"/>.</param>
        /// <param name="param">The parameters to pass, if any.<see cref="object"/>.</param>
        /// <param name="transaction">The transaction<see cref="IDbTransaction"/>.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="telemetryClient">The telemetryClient<see cref="TelemetryClient"/>.</param>
        /// <returns>The result of each query, the result of each query is a collection.</returns>
        public static async Task<DapperQueryMultipleResults> QueryMultipleAsyncConnection(
            this IDbConnection dbconnection,
            IEnumerable<DapperQuery> dapperQueries,
            IDatabaseEncryption databaseEncryption,
            bool validateKeyForDecryption = true,
            ILogger logger = null,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null,
            TelemetryClient telemetryClient = null)
        {
            List<string> sqlQueries = new List<string>();
            List<Type> queryReturnTypes = new List<Type>();
            foreach (var dapperQuery in dapperQueries)
            {
                sqlQueries.Add(dapperQuery.Query);
                queryReturnTypes.Add(dapperQuery.ReturnType);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            Stopwatch watch = Stopwatch.StartNew();
            SetCommandTimeout(ref commandTimeout);

            var multipleSql = string.Join(";", sqlQueries.ToArray());
            logger.LogDebug($"{LoggerHelper.SanitizeValue(new { multipleQuery = multipleSql })}.");
            var multipleResult = _sqlPolicy != null ?
                await _sqlPolicy.ExecuteAsync(async () => await dbconnection.QueryMultipleAsync(multipleSql, param, transaction, commandTimeout, commandType))
                :
                await dbconnection.QueryMultipleAsync(multipleSql, param, transaction, commandTimeout, commandType);

            if (databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = validateKeyForDecryption;
            }

            DapperQueryMultipleResults dapperQueryMultipleResults = new DapperQueryMultipleResults(queryReturnTypes);
            foreach (var returnType in queryReturnTypes)
            {
                var queryResult = await multipleResult.ReadAsync(returnType);
                queryResult = DecryptQueryResults<object>(queryResult, databaseEncryption);
                databaseEncryption.ValidateKeyForDecryption = true;
                dapperQueryMultipleResults.AddQueryResult(queryResult);
            }

            if (databaseEncryption != null)
            {
                databaseEncryption.ValidateKeyForDecryption = true;
            }

            watch.Stop();
            TrackQuery(now, watch.Elapsed, multipleSql, "QueryMultipleAsync", telemetryClient, param);
            return dapperQueryMultipleResults;
        }

        /// <summary>
        /// Set the Exos Retry Sql Policy.
        /// </summary>
        /// <param name="policyRegistry">The Policy Registry.</param>
        public static void SetExosRetrySqlPolicy(IPolicyRegistry<string> policyRegistry) => _sqlPolicy = policyRegistry.Get<AsyncPolicy>(PolicyRegistryKeys.SqlResiliencyPolicy);

        /// <summary>
        /// Set the wait time (in seconds) before terminating
        /// the attempt to execute a command.
        /// </summary>
        /// <param name="commandTimeout">Time in seconds.</param>
        public static void SetCommandTimeout(int commandTimeout) => _commandTimeout = commandTimeout;

        /// <summary>
        /// Add Multi-Tenancy where condition to a specific position in the query.
        /// </summary>
        /// <param name="sql">SQL query to add the Multi-Tenancy condition.</param>
        /// <param name="tenancyPlaceHolder">Position in where the Multi-Tenancy condition will be added.</param>
        /// <param name="tenancyWhereClause">Tenancy Condition.</param>
        /// <returns>A query with a Multi-Tenancy condition.</returns>
        private static string AddTenancyWhereClause(string sql, TenancyPlaceHolder tenancyPlaceHolder, string tenancyWhereClause)
        {
            switch (tenancyPlaceHolder)
            {
                case TenancyPlaceHolder.None:
                    sql += tenancyWhereClause;
                    break;
                case TenancyPlaceHolder.UseTenancyPlaceHolder:
                    sql = string.Format(CultureInfo.InvariantCulture, sql, tenancyWhereClause);
                    break;
                default:
                    sql += tenancyWhereClause;
                    break;
            }

            return sql;
        }

        private static string ApplyQueryOptions(string sql, QueryOption queryOption)
        {
            switch (queryOption)
            {
                case QueryOption.None:
                    break;
                case QueryOption.OptimizeForUnknown:
                    sql += QUERYHINTOPTIMIZEUNKNOWN;
                    break;
                case QueryOption.Recompile:
                    sql += QUERYHINTRECOMPILE;
                    break;
                default:
                    break;
            }

            return sql;
        }

        private static IEnumerable<T> DecryptQueryResults<T>(IEnumerable<T> queryResult, IDatabaseEncryption databaseEncryption)
        {
            if (queryResult.Any() && databaseEncryption != null)
            {
                foreach (var result in queryResult)
                {
                    databaseEncryption.DecryptEntityFrameworkObject<T>(result);
                }
            }

            return queryResult;
        }

        private static T DecryptResult<T>(T result, IDatabaseEncryption databaseEncryption)
        {
            if (result != null && databaseEncryption != null)
            {
                databaseEncryption.DecryptEntityFrameworkObject<T>(result);
            }

            return result;
        }

        private static void SetCommandTimeout(ref int? commandTimeout)
        {
            if (commandTimeout == null)
            {
                if (_commandTimeout > 0)
                {
                    commandTimeout = _commandTimeout;
                }
            }
        }
    }
}
#pragma warning restore CA1062 // Validate arguments of public methods