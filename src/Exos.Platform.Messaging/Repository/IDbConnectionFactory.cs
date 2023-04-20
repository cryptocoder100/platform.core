#pragma warning disable CS0618 // Type or member is obsolete
namespace Exos.Platform.Messaging.Repository
{
    using System;
    using System.Data;
    using Exos.Platform.AspNetCore.Authentication;
    using Microsoft.Data.SqlClient;

    /// <summary>
    /// Database Connection Factory.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Get Database connection.
        /// </summary>
        /// <returns>Database Connection.</returns>
        IDbConnection GetConnection();
    }

    /// <inheritdoc/>
    public class MessagingDbContext : IDbConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingDbContext"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string to open a connection to the SQL Server database.</param>
        public MessagingDbContext(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <inheritdoc/>
        public IDbConnection GetConnection()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);

            // If a password is supplied in the connection string then just make the connection
            if (!string.IsNullOrEmpty(builder.Password))
            {
                return new SqlConnection(_connectionString);
            }
            else
            {
                // Otherwise, use managed identities
                var conn = new SqlConnection(_connectionString);

                // conn.AccessToken = new ExosAzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/", "fnfms.onmicrosoft.com").Result;
                conn.AccessToken = new ExosAzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;

                return conn;
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete