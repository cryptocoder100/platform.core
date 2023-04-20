namespace Exos.MinimalPlatformApi.Repositories
{
    using System.Threading.Tasks;
    using Exos.MinimalPlatformApi.Models;
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Interface to the values collection.
    /// </summary>
    public interface IValuesRepository
    {
        /// <summary>
        /// Insert a value into the values collection.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>The value inserted with the identification upoated.</returns>
        Task<ValueModel> CreateValueAsync(ValueModel value);

        /// <summary>
        /// Retrieve a value from the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to retrieve.</param>
        /// <returns>The value retrieved, or null if not found.</returns>
        Task<ValueModel> ReadValueAsync(string id);

        /// <summary>
        /// Replace a value in the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to replace.</param>
        /// <param name="value">The value to replace with.</param>
        /// <returns>The replaced value with the identification updated, or null if not found.</returns>
        Task<ValueModel> UpdateValueAsync(string id, ValueModel value);

        /// <summary>
        /// Remove a value from the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to remove.</param>
        /// <returns>True if the value was removed, or false if not found.</returns>
        Task<bool> DeleteValueAsync(string id);

        /// <summary>
        /// Query the values collection.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The query results contained in a ListModel.</returns>
        Task<ListModel<ValueModel>> QueryValuesAsync(ValueQueryModel query);
    }
}
