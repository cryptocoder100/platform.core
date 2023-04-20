#pragma warning disable SA1402 // File may only contain a single type
namespace Exos.Platform.TenancyHelper.PersistenceService
{
    using System;

    /// <inheritdoc/>
    public class RepositoryOptions : IRepositoryOptions
    {
        /// <inheritdoc/>
        public string AuthKey { get; set; }

        /// <inheritdoc/>
        public string Collection { get; set; }

        /// <inheritdoc/>
        public string Database { get; set; }

        /// <inheritdoc/>
        public Uri Endpoint { get; set; }

        /// <inheritdoc/>
        public string ConnectionMode { get; set; }

        /// <inheritdoc/>
        public string PartitionKey { get; set; }

        /// <inheritdoc/>
        public bool ForceConcurrencyCheck { get; set; }

        /// <inheritdoc/>
        public bool ApplyDocumentPolicy { get; set; }

        /// <inheritdoc/>
        public string PolicyDocumentsCacheProvider { get; set; }

        /// <inheritdoc/>
        public string AccessTokenEndpoint { get; set; }

        /// <inheritdoc/>
        public Uri DatabaseKeysUrl { get; set; }

        /// <inheritdoc/>
        public string ConsistencyLevel { get; set; }

        /// <inheritdoc/>
        public bool CaptureQueryMetrics { get; set; }

        /// <inheritdoc/>
        public bool CaptureIndexMetrics { get; set; }

        /// <inheritdoc/>
        public bool CaptureRequestDiagnostics { get; set; }

        /// <inheritdoc/>
        public int PolicyTenancyExpirationInMinutes { get; set; }
    }

    /// <inheritdoc/>
    public class RepositoryOptionsInstance1 : RepositoryOptions
    {
    }

    /// <inheritdoc/>
    public class RepositoryOptionsInstance2 : RepositoryOptions
    {
    }

    /// <inheritdoc/>
    public class RepositoryOptionsInstance3 : RepositoryOptions
    {
    }

    /// <inheritdoc/>
    public class RepositoryOptionsInstance4 : RepositoryOptions
    {
    }

    /// <inheritdoc/>
    public class RepositoryOptionsInstance5 : RepositoryOptions
    {
    }

    /// <inheritdoc/>
    public class RepositoryOptionsInstance6 : RepositoryOptions
    {
    }
}
#pragma warning restore SA1402 // File may only contain a single type
