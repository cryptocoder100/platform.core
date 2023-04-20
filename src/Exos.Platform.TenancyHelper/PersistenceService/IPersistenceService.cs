namespace Exos.Platform.TenancyHelper.PersistenceService
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.Models;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    ///  Service  which all the typical cosmos call will be routed,
    ///  it will inject necessary tenant info and will take care of optimistic concurrency/Audit model.
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Gets the Repository Configuration..
        /// </summary>
        IRepositoryOptions RepositoryOptions { get; }

        /// <summary>
        /// Gets the PolicyHelper.
        /// </summary>
        IPolicyHelper PolicyHelper { get; }

        /// <summary>
        /// Create a Document asynchronously.
        /// </summary>
        /// <param name="document">Document to create.</param>
        /// <returns>Created Document.</returns>
        Task<ResourceResponse<Document>> CreateDocumentAsync(BaseModel document);

        /// <summary>
        /// Create a document asynchronously.
        /// </summary>
        /// <param name="document">Document to create.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Created Document.</returns>
        Task<ResourceResponse<Document>> CreateDocumentAsync(BaseModel document, IPolicyContext policyContext = null);

        /// <summary>
        /// Replace (update) a document asynchronously.
        /// </summary>
        /// <param name="docToReplace">Document to Replace.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Replaced Document.</returns>
        Task<ResourceResponse<Document>> ReplaceDocumentAsync(BaseModel docToReplace, IPolicyContext policyContext = null);

        /// <summary>
        /// Replace (update) a document asynchronously.
        /// </summary>
        /// <param name="id">Document Id.</param>
        /// <param name="docToReplace">Document to Replace.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Replaced Document.</returns>
        Task<ResourceResponse<Document>> ReplaceDocumentAsync(string id, BaseModel docToReplace, IPolicyContext policyContext = null);

        /// <summary>
        /// Replace (update) a document asynchronously.
        /// </summary>
        /// <param name="id">Document Id.</param>
        /// <param name="docToReplace">Document to Replace.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>Replaced Document.</returns>
        Task<ResourceResponse<Document>> ReplaceDocumentNative(string id, dynamic docToReplace, IPolicyContext policyContext = null);

        /// <summary>
        /// Search a Document
        /// This applies Servicer filter using equals clause.
        /// </summary>
        /// <typeparam name="T">Document Model.</typeparam>
        /// <typeparam name="TResult">Result Model.</typeparam>
        /// <param name="queryWriter">StringWriter.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="documentAlias">Document Alias.</param>
        /// <param name="orderBy">Order By.</param>
        /// <param name="feedOptions">FeedOptions.</param>
        /// <param name="tenantWhereClausePlaceHolderRef">Element to indicate where to insert the tenancy condition.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>PlatformResponse Model.</returns>
        Task<PlatformResponse<TResult>> ExecuteNextAsync<T, TResult>(
            StringWriter queryWriter,
            SqlParameterCollection sqlParameterCollection,
            string documentAlias,
            string orderBy = null,
            FeedOptions feedOptions = null,
            string tenantWhereClausePlaceHolderRef = null,
            IPolicyContext policyContext = null);

        /// <summary>
        /// Search a Document
        /// This applies Servicer filter using equals clause.
        /// </summary>
        /// <typeparam name="T">Document Model.</typeparam>
        /// <typeparam name="TResult">Result Model.</typeparam>
        /// <param name="queryWriter">StringWriter.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="documentAlias">Document Alias.</param>
        /// <param name="orderBy">Order By.</param>
        /// <param name="feedOptions">FeedOptions.</param>
        /// <param name="tenantWhereClausePlaceHolderRef">Element to indicate where to insert the tenancy condition.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <returns>PlatformResponse Model.</returns>
        Task<PlatformResponse<TResult>> ExecuteNextAsyncForSearches<T, TResult>(
            StringWriter queryWriter,
            SqlParameterCollection sqlParameterCollection,
            string documentAlias,
            string orderBy = null,
            FeedOptions feedOptions = null,
            string tenantWhereClausePlaceHolderRef = null,
            IPolicyContext policyContext = null);

        /// <summary>
        /// Search a Document.
        /// </summary>
        /// <typeparam name="T">Document Model.</typeparam>
        /// <typeparam name="TResult">Result Model.</typeparam>
        /// <param name="queryWriter">StringWriter.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="feedOptions">FeedOptions.</param>
        /// <returns>PlatformResponse Model.</returns>
        Task<PlatformResponse<TResult>> ExecuteNextAsync<T, TResult>(
            StringWriter queryWriter,
            SqlParameterCollection sqlParameterCollection,
            FeedOptions feedOptions = null);

        /// <summary>
        /// Search a Document.
        /// </summary>
        /// <typeparam name="T">Document Model.</typeparam>
        /// <typeparam name="TResult">Result Model.</typeparam>
        /// <param name="queryWriter">StringWriter.</param>
        /// <param name="sqlParameterCollection">SqlParameterCollection.</param>
        /// <param name="feedOptions">FeedOptions.</param>
        /// <returns>PlatformResponse Model.</returns>
        Task<PlatformResponse<TResult>> ExecuteNextNativeAsync<T, TResult>(
            StringWriter queryWriter,
            SqlParameterCollection sqlParameterCollection,
            FeedOptions feedOptions = null);

        /// <summary>
        /// Read a Document.
        /// </summary>
        /// <typeparam name="T">Document Model to Read.</typeparam>
        /// <param name="id">Document Id.</param>
        /// <param name="requestOptions">RequestOptions.</param>
        /// <returns>Document Model.</returns>
        Task<DocumentResponse<T>> ReadDocumentAsync<T>(string id, RequestOptions requestOptions);

        /// <summary>
        /// Execute Stored Procedure.
        /// </summary>
        /// <typeparam name="TValue">Document Model.</typeparam>
        /// <param name="storedProcId">Stored Procedure Id.</param>
        /// <param name="allDocuments">Base Model.</param>
        /// <param name="requestOptions">RequestOptions.</param>
        /// <param name="procedureParams">Parameters.</param>
        /// <returns>Store Procedure Response.</returns>
        Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(
            string storedProcId,
            List<BaseModel> allDocuments,
            RequestOptions requestOptions = null,
            params dynamic[] procedureParams);

        /// <summary>
        /// Execute Stored Procedure.
        /// It will apply tenancy condition.
        /// </summary>
        /// <typeparam name="TValue">Document Model.</typeparam>
        /// <param name="storedProcId">Stored Procedure Id.</param>
        /// <param name="policyContext">Policy Context.</param>
        /// <param name="allDocuments">Base Model.</param>
        /// <param name="requestOptions">RequestOptions.</param>
        /// <param name="procedureParams">Parameters.</param>
        /// <returns>Store Procedure Response.</returns>
        Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(
            string storedProcId,
            IPolicyContext policyContext,
            List<BaseModel> allDocuments,
            RequestOptions requestOptions = null,
            params dynamic[] procedureParams);
    }

    /// <inheritdoc/>
    public interface IPersistenceServiceInstance1 : IPersistenceService
    {
    }

    /// <inheritdoc/>
    public interface IPersistenceServiceInstance2 : IPersistenceService
    {
    }

    /// <inheritdoc/>
    public interface IPersistenceServiceInstance3 : IPersistenceService
    {
    }

    /// <inheritdoc/>
    public interface IPersistenceServiceInstance4 : IPersistenceService
    {
    }

    /// <inheritdoc/>
    public interface IPersistenceServiceInstance5 : IPersistenceService
    {
    }

    /// <inheritdoc/>
    public interface IPersistenceServiceInstance6 : IPersistenceService
    {
    }
}
