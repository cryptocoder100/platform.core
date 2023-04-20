#pragma warning disable CA2227 // Collection properties should be read only
namespace Exos.Platform.TenancyHelper.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Return a collection of T documents.
    /// </summary>
    /// <typeparam name="T">Model To Return.</typeparam>
    public class PlatformResponse<T>
    {
        /// <summary>
        /// Gets or sets the List of Documents.
        /// </summary>
        public List<T> List { get; set; }

        /// <summary>
        /// Gets or sets the continuation token, used to retrieve the next set of documents.
        /// </summary>
        public string ResponseContinuation { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only