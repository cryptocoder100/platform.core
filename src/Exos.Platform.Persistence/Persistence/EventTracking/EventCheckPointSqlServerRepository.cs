#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapper;
    using Exos.Platform.Persistence.Entities;
    using Exos.Platform.Persistence.EventPoller;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc/>
    public class EventPublishCheckPointSqlServerRepository<T> : IEventPublishCheckPointSqlRepository<T> where T : EventPublishCheckPointEntity
    {
        private readonly ILogger<EventPublishCheckPointSqlServerRepository<T>> _logger;

        private readonly DbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublishCheckPointSqlServerRepository{T}"/> class.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        public EventPublishCheckPointSqlServerRepository(IServiceProvider serviceProvider, ILogger<EventPublishCheckPointSqlServerRepository<T>> logger)
        {
            _dbContext = serviceProvider.GetService<PlatformDbContext>();
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task CreateEventCheckPoints(List<T> checkPoints)
        {
            if (checkPoints is null)
            {
                throw new ArgumentNullException(nameof(checkPoints));
            }

            var dataSet = MapToDataTable(checkPoints);
            var connection = (Microsoft.Data.SqlClient.SqlConnection)_dbContext.Database.GetDbConnection();
            {
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                }

                using (var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "Chg.EventPublishCheckPoint";
                    try
                    {
                        bulkCopy.BatchSize = checkPoints.Count;
                        bulkCopy.BulkCopyTimeout = 0;
                        await bulkCopy.WriteToServerAsync(dataSet).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (dataSet != null)
                        {
                            dataSet.Dispose();
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<T> UpdateEventCheckPoint(T checkPoint)
        {
            _dbContext.Update(checkPoint);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return checkPoint;
        }

        /// <inheritdoc/>
        public async Task DeleteEventCheckPoints(string deleteQuery, int[] eventIds, byte processId)
        {
            if (eventIds != null && eventIds.Length > 0)
            {
                using var connection = _dbContext.Database.GetDbConnection();
                await connection.ExecuteAsync(deleteQuery, new { EventIds = eventIds, ProcessId = processId }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<int> HardDeleteEventCheckpoints(string eventPublishCheckPoinHardDeleteQuery, int[] eventIds)
        {
            int deletedRows = 0;
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                deletedRows = await connection.ExecuteAsync(eventPublishCheckPoinHardDeleteQuery, new { EventIds = eventIds }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database exception in EventPublishCheckPointSqlServerRepository  {message} - method HardDeleteEvents - {query}", e.Message, eventPublishCheckPoinHardDeleteQuery);
                throw;
            }

            return deletedRows;
        }

        /// <inheritdoc/>
        public async Task<List<int>> GetEventIdsToBeDeleted(string eventIdsToBeDeletedQuery)
        {
            try
            {
                using var connection = _dbContext.Database.GetDbConnection();
                var eventIdsToBeDeleted = await connection.QueryAsync<int>(eventIdsToBeDeletedQuery).ConfigureAwait(false);
                return eventIdsToBeDeleted?.AsList<int>();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database exception in EventSqlServerRepository  {message} - method GetEventIdsToBeDeleted - {query}", e.Message, eventIdsToBeDeletedQuery);
                throw;
            }
        }

        private static DataTable MapToDataTable(List<T> checkPoints)
        {
            var dt = new DataTable();
            var checkPoint = checkPoints.FirstOrDefault();

            string eventIdColName = nameof(checkPoint.EventId);
            string processIdColName = nameof(checkPoint.ProcessId);
            string isActiveColName = nameof(checkPoint.IsActive);
            string createdByColName = nameof(checkPoint.CreatedBy);
            string createdDateColName = nameof(checkPoint.CreatedDate);
            string lastUpdatedByColName = nameof(checkPoint.LastUpdatedBy);
            string lastUpdatedDateColName = nameof(checkPoint.LastUpdatedDate);
            string clientTenantIdColName = "ClientTenantId";
            string subClientTenantIdColName = "SubClientTenantId";
            string vendorTenantIdColName = "VendorTenantId";
            string subContractorTenantIdColName = "SubContractorTenantId";
            string servicerTenantIdColName = "ServicerTenantId";
            string servicerGroupTenantIdColName = "ServicerGroupTenantId";
            string versionColName = nameof(checkPoint.Version);

            dt.Columns.Add("EventPublishCheckPointId", typeof(int));
            dt.Columns.Add(eventIdColName, typeof(int));
            dt.Columns.Add(processIdColName, typeof(byte));
            dt.Columns.Add(isActiveColName, typeof(bool));
            dt.Columns.Add(createdByColName, typeof(string));
            dt.Columns.Add(createdDateColName, typeof(DateTime));
            dt.Columns.Add(lastUpdatedByColName, typeof(string));
            dt.Columns.Add(lastUpdatedDateColName, typeof(DateTime));
            dt.Columns.Add(clientTenantIdColName, typeof(int));
            dt.Columns.Add(subClientTenantIdColName, typeof(int));
            dt.Columns.Add(vendorTenantIdColName, typeof(int));
            dt.Columns.Add(subContractorTenantIdColName, typeof(int));
            dt.Columns.Add(servicerTenantIdColName, typeof(short));
            dt.Columns.Add(servicerGroupTenantIdColName, typeof(short));
            dt.Columns.Add(versionColName, typeof(DateTime));

            foreach (var lineItem in checkPoints)
            {
                var row = dt.NewRow();

                row[eventIdColName] = lineItem.EventId;
                row[processIdColName] = lineItem.ProcessId;
                row[isActiveColName] = 1;
                row[createdByColName] = lineItem.CreatedBy;
                row[createdDateColName] = DateTime.UtcNow;
                row[lastUpdatedByColName] = lineItem.LastUpdatedBy;
                row[lastUpdatedDateColName] = (object)lineItem.LastUpdatedDate ?? DBNull.Value;
                row[clientTenantIdColName] = 0;
                row[subClientTenantIdColName] = 0;
                row[vendorTenantIdColName] = 0;
                row[subContractorTenantIdColName] = 0;
                row[servicerTenantIdColName] = 0;
                row[servicerGroupTenantIdColName] = 0;
                row[versionColName] = DateTime.UtcNow;
                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName