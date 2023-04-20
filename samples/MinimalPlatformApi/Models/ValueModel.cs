namespace Exos.MinimalPlatformApi.Models
{
    /// <summary>
    /// Value Model.
    /// </summary>
    public class ValueModel
    {
        /// <summary>
        /// Gets ModelType.
        /// </summary>
        public static string ModelType => "value";

        /// <summary>
        /// Gets or sets Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Value.
        /// </summary>
        public string Value { get; set; }
    }
}
