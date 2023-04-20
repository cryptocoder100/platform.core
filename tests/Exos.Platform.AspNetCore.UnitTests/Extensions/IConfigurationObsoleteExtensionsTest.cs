#pragma warning disable CS0618
#pragma warning disable CA1822
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.AspNetCore.UnitTests.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography.X509Certificates;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IConfigurationObsoleteExtensionsTest
    {
        [TestMethod]
        [Ignore]
        public void InjectTokenizedVariablesTest()
        {
            var webHost = WebHost.CreateDefaultBuilder()
               .UseStartup<IWebHostBuildExtensionsTest>()
               // .UsePlatformExternalJsonFilesConfiguration()
               .UsePlatformConfigurationDefaults()
               .ConfigureServices(ConfigureServices)
               .Build();
            Assert.IsNotNull(webHost);
            var config = webHost.Services.GetService<IConfiguration>();

            string azureConnString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Azure);
            string azureSQL_MessageDb = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Messaging);
            azureSQL_MessageDb = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Messaging);

            // string MessageDb = config.GetTokenizedValue<string>("AzureSQL:ReadWriteConnectionString");
            string msgEnvironment = config.GetValue<string>("Messaging:Environment");
            CustomizedRedisCacheOptions redisOptions = new CustomizedRedisCacheOptions();
            config.GetSection("Redis").Bind(redisOptions);

            // string azureSQL_ReadWriteConnectionStringTokenized = config.GetTokenizedValue<string>("AzureSQL:ReadWriteConnectionString");
            // string azureConnString = config.GetConnectionString(IConfigurationExtensions.ConnectionStringType.Azure);
            string iaasConnString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.IaaS);
            string messagingConnString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Messaging);
            string ictConnString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Ict);
            string redisConnString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Redis);

            string appInsights_InstrumentationKey = config.GetValue<string>("ApplicationInsights:InstrumentationKey");
            string azureSQL_ReadWriteConnectionString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Azure);
            // string azureSQL_MessageDb = config.GetConnectionString(IConfigurationExtensions.ConnectionStringType.Messaging);
            string cosmos_ReadKey = config.GetValue<string>("Cosmos:ReadKey");
            string cosmos_ReadWriteKey = config.GetValue<string>("Cosmos:ReadWriteKey");
            string iaaSSQL_ReadWriteConnectionString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.IaaS);
            string jwt_SigningKey = config.GetValue<string>("JWT:SigningKey");
            string redis_ConnectionPoolEnabled = config.GetValue<string>("Redis:ConnectionPoolEnabled");
            string redis_MaxRetries = config.GetValue<string>("Redis:MaxRetries");
            string redis_PoolSize = config.GetValue<string>("Redis:PoolSize");
            string redis_ReadWriteConnectionString = config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Redis);
            string threadPoolOptions_MinCompletionPortThreads = config.GetValue<string>("ThreadPoolOptions:MinCompletionPortThreads");
            string threadPoolOptions_MinWorkerThreads = config.GetValue<string>("ThreadPoolOptions:MinWorkerThreads");

            var cosmosSection = config.GetTokenizedSection("Cosmos");
            var documentRepositorySection = config.GetTokenizedSection("Cosmos").GetSection("UserManagementDocumentRepository");
            var cosmosAuthKey = config.GetValue<string>("Cosmos:UserManagementDocumentRepository:AuthKey");
            string cosmosEndpoint = config.GetValue<string>("Cosmos:UserManagementDocumentRepository:Endpoint");
            string azureSQLReadWriteConnectionString = config.GetValue<string>("AzureSQL:ReadWriteConnectionString");
            string azureSQLMessageDb = config.GetValue<string>("AzureSQL:MessageDb");
            string iaaSSQLReadWriteConnectionString = config.GetValue<string>("IaaSSQL:ReadWriteConnectionString");
            string iaaSSQLMessageDb = config.GetValue<string>("IaaSSQL:MessageDb");

            // Check that we can both 1) get Keys from Key Vault, and 2) inject the Keys into each of the settings
            Assert.IsFalse(config.GetValue<string>("ApplicationInsights:InstrumentationKey") == "fromappsettings");
            Assert.IsFalse(config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Azure) == "fromappsettings");
            Assert.IsFalse(config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.IaaS) == "fromappsettings");
            Assert.IsFalse(config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Messaging) == "fromappsettings");
            Assert.IsFalse(config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Ict) == "fromappsettings");
            Assert.IsFalse(config.GetConnectionString(IConfigurationObsoleteExtensions.ConnectionStringType.Redis) == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Cosmos:Endpoint") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Cosmos:ReadKey") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Cosmos:ReadWriteKey") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("JWT:SigningKey") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Redis:ConnectionPoolEnabled") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Redis:MaxRetries") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Redis:PoolSize") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("Redis:ReadWriteConnectionString") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("ThreadPoolOptions:MinCompletionPortThreads") == "fromappsettings");
            Assert.IsFalse(config.GetValue<string>("ThreadPoolOptions:MinWorkerThreads") == "fromappsettings");

            Assert.AreEqual(config.GetValue<string>("Cosmos:ReadWriteKey"), config.GetValue<string>("Cosmos:UserManagementDocumentRepository:AuthKey"));
            Assert.AreEqual(config.GetValue<string>("Cosmos:Endpoint"), config.GetValue<string>("Cosmos:UserManagementDocumentRepository:Endpoint"));
        }

        [TestMethod]
        [Ignore]
        public void KeyvaultCertificateTest()
        {
            var webHost = WebHost.CreateDefaultBuilder()
               .UseStartup<IWebHostBuildExtensionsTest>()
               .UsePlatformConfigurationDefaults()
               .ConfigureServices(ConfigureServices)
               .Build();
            Assert.IsNotNull(webHost);
            var config = webHost.Services.GetService<IConfiguration>();

            X509Certificate2 certificate = null;
            byte[] publicKey = null;
            System.Security.Cryptography.AsymmetricAlgorithm privateKey = null;
            certificate = new X509Certificate2(Convert.FromBase64String(config.GetValue<string>("zachtest")), (string)null);
            publicKey = certificate.GetPublicKey();
            privateKey = certificate.GetRSAPrivateKey();

            Assert.IsNotNull(certificate);
            Assert.IsNotNull(publicKey);
            Assert.IsNotNull(privateKey);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ISecureEncryption, AesEncryption>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class CustomizedRedisCacheOptions : Microsoft.Extensions.Options.IOptions<CustomizedRedisCacheOptions>
#pragma warning restore SA1402 // File may only contain a single type
    {
        /// <summary>
        /// Gets or sets keyPrefix.
        /// </summary>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Gets or sets expiryDay.
        /// </summary>
        public int ExpiryDay { get; set; }

        /// <summary>
        /// Gets or sets redisConnString.
        /// </summary>
        public string ReadWriteConnectionString { get; set; }

        /// <summary>
        /// Gets or sets pagenationExpireSeconds.
        /// </summary>
        public int PagenationExpireSeconds { get; set; }

        /// <summary>
        /// Gets value.
        /// </summary>
        public CustomizedRedisCacheOptions Value
        {
            get { return this; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether flag to use In-memory cache instead of Redis cache.
        /// </summary>
        public bool UseInMemoryCache { get; set; }

        public CustomizedRedisCacheOptions Get(string name)
        {
            return Get(name);
        }
    }
}
#pragma warning restore CS0618
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1801 // Review unused parameters