namespace Exos.Platform.AspNetCore.Resiliency.Policies
{
    /// <summary>
    /// Define keys to use in Policy Registry.
    /// </summary>
    public static class PolicyRegistryKeys
    {
        /// <summary>
        /// Represent a Sql Resiliency Policy.
        /// </summary>
        public const string SqlResiliencyPolicy = "IExosRetrySqlPolicy";

        /// <summary>
        /// Represent a Blob Storage Policy.
        /// </summary>
        public const string BlobStorageResiliencyPolicy = "BlobStorageResiliencyPolicy";

        /// <summary>
        /// The HTTP request polly policy.
        /// </summary>
        public const string Http = "Http";
    }
}
