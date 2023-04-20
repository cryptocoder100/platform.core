namespace Exos.Platform.Persistence.GenericRepo
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapper;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Dapper based generic repo.
    /// </summary>
    public class DapperGenericRepo : IDapperGenericRepo
    {
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly IPolicyHelper _policyHelper;
        private readonly IPolicyContext _policyContext;
        private readonly ILogger<DapperGenericRepo> _logger;
        private readonly TelemetryClient _telemetryClient;
        private PropertyContainer _properties;
        private IDbConnection _sqlConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DapperGenericRepo"/> class.
        /// </summary>
        /// <param name="userHttpContextAccessorService"><see cref="IUserHttpContextAccessorService"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        /// <param name="policyHelper"><see cref="IPolicyHelper"/>.</param>
        /// <param name="policyContext"><see cref="IPolicyContext"/>.</param>
        public DapperGenericRepo(IUserHttpContextAccessorService userHttpContextAccessorService, ILogger<DapperGenericRepo> logger, TelemetryClient telemetryClient, IPolicyHelper policyHelper = null, IPolicyContext policyContext = null)
        {
            _userHttpContextAccessorService = userHttpContextAccessorService ?? throw new ArgumentNullException(nameof(userHttpContextAccessorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _policyHelper = policyHelper ?? throw new ArgumentNullException(nameof(policyHelper));
            _policyContext = policyContext ?? throw new ArgumentNullException(nameof(policyContext));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        /// <inheritdoc/>
        public void ProvideSqlConnection(IDbConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        /// <inheritdoc/>
        public async Task Save<T>(T entity, int id) where T : class
        {
            try
            {
                _properties = new PropertyContainer().ParseProperties<T>(entity);
                if (id > 0)
                {
                    var existingRecord = await SelectAsync<T>(entity).ConfigureAwait(false);
                    if (existingRecord == null || existingRecord.FirstOrDefault() == null)
                    {
                        await InsertAsync<T>(entity).ConfigureAwait(false);
                    }
                    else
                    {
                        await UpdateAsync<T>(entity).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _properties = null;
            }
        }

        /// <summary>
        /// Get Items.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="sql">Sql statement.</param>
        /// <param name="parameters">Query parameters.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task<IEnumerable<T>> GetItemsAsync<T>(CommandType commandType, string sql, object parameters = null)
        {
            return _sqlConnection.QueryAsyncTenancyConnection<T>(sql, null, _policyHelper, _logger, parameters, null, null, commandType, _telemetryClient);
        }

        /// <summary>
        /// Execute a Query.
        /// </summary>
        /// <param name="commandType"><see cref="CommandType"/>.</param>
        /// <param name="sql">Sql statement.</param>
        /// <param name="parameters">Query parameters.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task<IEnumerable<int>> ExecuteAsync(CommandType commandType, string sql, object parameters = null)
        {
            return _sqlConnection.QueryAsyncTenancyConnection<int>(sql, null, _policyHelper, _logger, parameters, null, null, commandType, _telemetryClient);
        }

        /// <summary>
        /// Select items that match criteria.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="criteria">Select criteria.</param>
        /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task<IEnumerable<T>> SelectAsync<T>(T criteria)
        {
            var crit = criteria;
            var properties = _properties;
            if ((int)properties.IdPairs.FirstOrDefault().Value == 0)
            {
                return Task.FromResult<IEnumerable<T>>(null);
            }

            var sqlPairs = GetSqlPairs(properties.IdNames, " AND ");

            var sql = "SELECT * FROM " + properties.TableName + " WHERE " + sqlPairs;
            return GetItemsAsync<T>(CommandType.Text, sql, properties.IdPairs);
        }

        /// <summary>
        /// Insert an object.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="entity">Object to Insert.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task<IEnumerable<int>> InsertAsync<T>(T entity)
        {
            var ent = entity;
            var propertyContainer = _properties;
            var sql = "INSERT INTO " + propertyContainer.TableName + " (" + string.Join(", ", propertyContainer.AllNames) + ") VALUES (@" + string.Join(", @", propertyContainer.AllNames) + ")";
            return _sqlConnection.QueryAsyncTenancyConnection<int>(sql, null, _policyHelper, _logger, propertyContainer.AllPairs, null, null, CommandType.Text, _telemetryClient);
        }

        /// <summary>
        /// Insert an object.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="entity">Object to Insert.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task InsertAsyncIdentity<T>(T entity)
        {
            var propertyContainer = _properties;
            var sql = "INSERT INTO " + _properties.TableName + " (" + string.Join(", ", propertyContainer.AllNames) + ") VALUES(@" + string.Join(", @", propertyContainer.AllNames) + ") SELECT CAST(scope_identity() AS int)";
            var id = await _sqlConnection.QueryAsync<int>(sql, propertyContainer.AllPairs, commandType: CommandType.Text).ConfigureAwait(false);
            SetId(entity, id.First(), propertyContainer.IdPairs);
        }

        /// <summary>
        /// Update an object.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="entity">Object to Insert.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task UpdateAsync<T>(T entity)
        {
            var ent = entity;
            var propertyContainer = _properties;
            var sqlIdPairs = GetSqlPairs(propertyContainer.IdNames);
            var sqlValuePairs = GetSqlPairs(propertyContainer.ValueNames);
            var sql = "UPDATE " + propertyContainer.TableName + " SET " + sqlValuePairs + " WHERE " + sqlIdPairs;
            return ExecuteAsync(CommandType.Text, sql, propertyContainer.AllPairs);
        }

        /// <summary>
        /// Delete an object.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="entity">Object to Insert.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected Task Delete<T>(T entity)
        {
            var ent = entity;
            var propertyContainer = _properties;
            var sqlIdPairs = GetSqlPairs(propertyContainer.IdNames);
            return ExecuteAsync(CommandType.Text, "DELETE FROM " + propertyContainer.TableName + " WHERE " + sqlIdPairs, propertyContainer.IdPairs);
        }

        /// <summary>
        /// Create a commaseparated list of value pairs on.
        /// the form: "key1=@value1, key2=@value2, ...".
        /// </summary>
        private static string GetSqlPairs(IEnumerable<string> keys, string separator = ", ")
        {
            var pairs = keys.Select(key => key + "=@" + key).ToList();
            return string.Join(separator, pairs);
        }

        private static void SetId<T>(T obj, int id, IDictionary<string, object> propertyPairs)
        {
            if (propertyPairs.Count == 1)
            {
                var propertyName = propertyPairs.Keys.First();
                var propertyInfo = obj.GetType().GetProperty(propertyName);
                if (propertyInfo.PropertyType == typeof(int))
                {
                    propertyInfo.SetValue(obj, id, null);
                }
            }
        }
    }
}
