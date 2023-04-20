#pragma warning disable CA2227 // Collection properties should be read only

namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Models a list of data returned in search requests.
    /// </summary>
    /// <typeparam name="T">The element type of the ListModel.</typeparam>
    public class ListModel<T>
    {
        /// <summary>
        /// Gets the model type. This always returns <see cref="ModelType.List" />..
        /// </summary>
        public ModelType Model { get; } = ModelType.List;

        /// <summary>
        /// Gets or sets a continuation token that can be used to query the next page of results.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Gets or sets the actual list of results.
        /// </summary>
        public List<T> Data { get; set; } = new List<T>();
    }
}
#pragma warning restore CA2227 // Collection properties should be read only