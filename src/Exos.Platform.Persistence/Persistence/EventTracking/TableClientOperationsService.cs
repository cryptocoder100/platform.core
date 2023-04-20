namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Exos.Platform.Persistence.EventPoller;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class TableClientOperationsService<T> : ITableClientOperationsService<T> where T : TableEntity, ITableEntity, new()
    {
        private static readonly string ApplicationName = Assembly.GetEntryAssembly().GetName().Name;
        private readonly EventPollerServiceSettings _blobConnectionRepository;
        private readonly ILogger<TableClientOperationsService<T>> _logger;
        private readonly Microsoft.Azure.Cosmos.Table.CloudStorageAccount _storageAccount;
        private readonly CloudTableClient _tableClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableClientOperationsService{T}"/> class.
        /// TableClientOperationsService.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="blobConnectionRepository">blobConnectionRepository.</param>
        public TableClientOperationsService(ILogger<TableClientOperationsService<T>> logger, IOptions<EventPollerServiceSettings> blobConnectionRepository)
        {
            if (blobConnectionRepository is null)
            {
                throw new ArgumentNullException(nameof(blobConnectionRepository));
            }

            _logger = logger;
            _blobConnectionRepository = blobConnectionRepository.Value;
            _storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(_blobConnectionRepository.EventsArchiveStorageReadWriteConnectionString);
            _tableClient = _storageAccount.CreateCloudTableClient();
            _tableClient.DefaultRequestOptions = new TableRequestOptions
            {
                RetryPolicy = new Microsoft.Azure.Cosmos.Table.ExponentialRetry(TimeSpan.FromSeconds(3), 20),
                LocationMode = Microsoft.Azure.Cosmos.Table.LocationMode.PrimaryThenSecondary,
                MaximumExecutionTime = TimeSpan.FromSeconds(20)
            };
        }

        /// <inheritdoc/>
        public async Task ExecuteBatchAsync(List<T> batchToSave)
        {
            try
            {
                if (batchToSave != null && batchToSave.Count > 0)
                {
                    CloudTable table = _tableClient.GetTableReference(ApplicationName + typeof(T).Name);
                    // await table.CreateIfNotExistsAsync();
                    TableBatchOperation batchOperation = new TableBatchOperation();

                    for (var i = 0; i < batchToSave.Count; i += 100)
                    {
                        var batchItems = batchToSave.Skip(i)
                                                 .Take(100)
                                                 .ToList();
                        var batch = new TableBatchOperation();
                        foreach (var item in batchItems)
                        {
                            batch.Insert(item);
                        }

                        await table.ExecuteBatchAsync(batch);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ExecuteBatch failed");
                throw;
            }
        }
    }
}
