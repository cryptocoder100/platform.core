namespace Exos.Platform.Persistence.EventTracking
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// ITableClientOperationsService.
    /// </summary>
    /// <typeparam name="T">T.</typeparam>
    public interface ITableClientOperationsService<T> where T : TableEntity, ITableEntity, new()
    {
        /// <summary>
        /// ExecuteBatchAsync.
        /// </summary>
        /// <param name="batchToSave">batchToSave.</param>
        /// <returns>Task.</returns>
        Task ExecuteBatchAsync(List<T> batchToSave);
    }
}