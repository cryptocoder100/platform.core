namespace Exos.Platform.AspNetCore.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Base model for request filtering.
    /// </summary>
    public class QueryModel
    {
        /// <summary>
        /// Gets or sets the number of results to return.
        /// Values can be between 1 and 100.
        /// The default is 10.
        /// </summary>
        [Range(1, 100)]
        public int Limit { get; set; } = 10;

        /// <summary>
        /// Gets or sets a continuation token the caller may have received in a previous request to select the next page of results.
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
