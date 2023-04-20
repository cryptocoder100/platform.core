namespace Exos.Platform.AspNetCore.Models
{
    using System;

    /// <summary>
    /// Represent an Entry in the Cache.
    /// </summary>
    [Serializable]
    public class CacheItem
    {
        /// <summary>
        /// Gets or sets the Cache Item Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Cache Item Data.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets Cache Item Expiration (Time To Live).
        /// </summary>
        public DateTime Expiration { get; set; }
    }
}
