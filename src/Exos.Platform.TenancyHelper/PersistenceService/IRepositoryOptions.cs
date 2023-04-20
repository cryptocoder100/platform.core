namespace Exos.Platform.TenancyHelper.PersistenceService
{
    using System;

    /// <summary>
    /// Configuration class to access Cosmos DB.
    /// </summary>
    public interface IRepositoryOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether Tenancy is applied or not.
        /// </summary>
        bool ApplyDocumentPolicy { get; set; }

        /// <summary>
        /// Gets or sets the authorization key to use to create the Azure Document client.
        /// </summary>
        string AuthKey { get; set; }

        /// <summary>
        /// Gets or sets the Azure DB Collection.
        /// </summary>
        string Collection { get; set; }

        /// <summary>
        /// Gets or sets the Azure Database.
        /// </summary>
        string Database { get; set; }

        /// <summary>
        ///  Gets or sets the service endpoint to use to create the Document client.
        /// </summary>
        Uri Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the connection mode to be used by the Document client.
        /// </summary>
        string ConnectionMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use Optimistic concurrency.
        /// </summary>
        bool ForceConcurrencyCheck { get; set; }

        /// <summary>
        /// Gets or sets the Azure DB Partition.
        /// </summary>
        string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the value of the provider for Policy Documents.
        /// </summary>
        string PolicyDocumentsCacheProvider { get; set; }

        /// <summary>
        /// Gets or sets the endpoint for retrieving a managed identity token.
        /// </summary>
        string AccessTokenEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the URL for retrieving the DB keys.
        /// </summary>
        Uri DatabaseKeysUrl { get; set; }

        /// <summary>
        /// Gets or sets the consistency level.
        /// </summary>
        string ConsistencyLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Microsoft.Azure.Documents.Client.FeedOptions.PopulateQueryMetrics" /> flag is set.
        /// </summary>
        bool CaptureQueryMetrics { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Microsoft.Azure.Documents.Client.FeedOptions.PopulateIndexMetrics" /> flag is set.
        /// </summary>
        bool CaptureIndexMetrics { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the RequestDiagnosticsString is sent to Application Insights.
        /// </summary>
        bool CaptureRequestDiagnostics { get; set; }

        /// <summary>
        /// Gets or sets the policy tenancy expiration in minutes.
        /// </summary>
        int PolicyTenancyExpirationInMinutes { get; set; }
    }
}