namespace Exos.MinimalPlatformApi.Models
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Value Query Model for request filtering.
    /// </summary>
    public class ValueQueryModel : QueryModel
    {
        /// <summary>
        /// Order By Condition.
        /// </summary>
        public enum OrderBy
        {
            /// <summary>
            /// Unsorted Condition.
            /// </summary>
            Unsorted,

            /// <summary>
            /// Order By Id Condition.
            /// </summary>
            Id,

            /// <summary>
            /// Order By Name Condition.
            /// </summary>
            Name,

            /// <summary>
            /// Order By Value Condition.
            /// </summary>
            Value,
        }

        /// <summary>
        /// Gets or sets NameContains.
        /// </summary>
        public string NameContains { get; set; }

        /// <summary>
        /// Gets or sets ValueContains.
        /// </summary>
        public string ValueContains { get; set; }

        /// <summary>
        /// Gets or sets OrderedBy.
        /// </summary>
        public OrderBy OrderedBy { get; set; }
    }
}
