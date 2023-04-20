namespace Exos.Platform.Messaging.Core
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;

    /// <summary>
    /// Defines the <see cref="IExosClientEntityPool{T}"/>.
    /// </summary>
    /// <typeparam name="T"><see cref="IClientEntity"/>.</typeparam>
    public interface IExosClientEntityPool<T> where T : IClientEntity
    {
        /// <summary>
        /// Determines whether the <see cref="IExosClientEntityPool{T}"/> contains the specified <typeparamref name="T"/> instance.
        /// </summary>
        /// <param name="clientName">The name to locate in the <see cref="IExosClientEntityPool{T}"/>.</param>
        /// <returns>True if the <see cref="ExosTopicClientPool"/> contains an <typeparamref name="T"/> instance with the specified name;
        /// otherwise false.</returns>
        bool ContainsClientEntity(string clientName);

        /// <summary>
        /// Adds a <typeparamref name="T"/> to the pool, if the name does not already exist.
        /// Returns the new client, or the existing client if the name exists.
        /// </summary>
        /// <param name="namespaceName">The namespace name used in the name of the client.</param>
        /// <param name="entityName">The entity name used in the name of the client.</param>
        /// <param name="retryPolicy">The retry policy to be applied to the client.</param>
        /// <returns>The <typeparamref name="T"/> instance.</returns>
        T GetClientEntity(string namespaceName, string entityName, RetryPolicy retryPolicy = null);

        /// <summary>
        /// Attempts to remove an instance of <typeparamref name="T"/> from the client pool.
        /// </summary>
        /// <param name="clientName">The name of the client to be removed.</param>
        /// <returns>True if client exists, removed and closed successfully, or client does not exist, otherwise false. </returns>
        Task<bool> TryCloseClientEntityAsync(string clientName);
    }
}