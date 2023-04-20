namespace Exos.Platform.TenancyHelper.PersistenceService
{
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Access DocumentClient for Azure Cosmos DB.
    /// </summary>
    public interface IDocumentClientAccessor
    {
        /// <summary>
        /// Gets DocumentClient.
        /// </summary>
        DocumentClient DocumentClient { get; }

        /// <summary>
        /// Gets RepositoryOptions to Access Azure Cosmos DB.
        /// </summary>
        RepositoryOptions RepositoryOptions { get; }
    }

    /// <inheritdoc/>
    public interface IDocumentClientAccessor1 : IDocumentClientAccessor
    {
    }

    /// <inheritdoc/>
    public interface IDocumentClientAccessor2 : IDocumentClientAccessor
    {
    }

    /// <inheritdoc/>
    public interface IDocumentClientAccessor3 : IDocumentClientAccessor
    {
    }

    /// <inheritdoc/>
    public interface IDocumentClientAccessor4 : IDocumentClientAccessor
    {
    }

    /// <inheritdoc/>
    public interface IDocumentClientAccessor5 : IDocumentClientAccessor
    {
    }

    /// <inheritdoc/>
    public interface IDocumentClientAccessor6 : IDocumentClientAccessor
    {
    }
}