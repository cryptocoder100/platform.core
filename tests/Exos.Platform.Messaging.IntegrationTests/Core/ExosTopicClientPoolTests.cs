#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.Messaging.IntegrationTests.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.AppConfiguration;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.Messaging.Core;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Primitives;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [ExcludeFromCodeCoverage]
    [Obsolete(" ExosAzureConfigurationResolutionProcessor.ProcessTokenResolution is obsolete")]
    public class ExosTopicClientPoolTests
    {
        private static TestServer _testServer;
        private static IConfiguration _configuration;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Publisher.json");

            _configuration = builder.Build();

            ExosAzureConfigurationResolutionProcessor
                .ProcessTokenResolution(_configuration, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            _testServer = new TestServer(new WebHostBuilder()
                .UsePlatformConfigurationDefaults()
                .UsePlatformLoggingDefaults()
                .UseConfiguration(_configuration)
                .UseStartup<PublisherTestStartup>());
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void FailoverConfig_GivenExceptionOverwriteFromConfiguration_ShouldHonorConfiguration()
        {
            // 4 exception names from appsettings
            MessageSection section = _testServer.Services.GetService<IOptions<MessageSection>>().Value;
            Assert.AreEqual(4, section.FailoverConfig.ExceptionNames.Count);

            // reset string refreshes hashset
            section.FailoverConfig.ExceptionNamesString = string.Empty;
            Assert.AreEqual(0, section.FailoverConfig.ExceptionNames.Count);

            // spaces are ignored
            section.FailoverConfig.ExceptionNamesString = "  , ,  ,  ";
            Assert.AreEqual(0, section.FailoverConfig.ExceptionNames.Count);
        }

        [TestMethod]
        public void GetTopicClient_GivenClientNotExists_ShouldCreateAndAddToPoolUsingString()
        {
            // Arrange
            ITokenProvider tokenProvider = _testServer.Services.GetService<ITokenProvider>();
            ExosTopicClientPool pool = new ExosTopicClientPool(tokenProvider);
            string namespaceName = _configuration["ServiceBus:ActiveConnectionString"];

            // Act
            TopicClient topicClient = pool.GetClientEntity(namespaceName, "Topic1");

            // Assert
            Assert.IsNotNull(topicClient);
        }

        [TestMethod]
        public void GetTopicClient_GivenClientNotExists_ShouldCreateAndAddToPoolUsingPlatformConnection()
        {
            // Arrange
            ITokenProvider tokenProvider = _testServer.Services.GetService<ITokenProvider>();
            ExosTopicClientPool pool = new ExosTopicClientPool(tokenProvider);
            string namespaceName = _configuration["ServiceBus:ActiveConnectionString"];

            // Act
            TopicClient topicClient = pool.GetClientEntity(namespaceName, "Topic1");

            // Assert
            Assert.IsNotNull(topicClient);
        }

        [TestMethod]
        public void GetTopicClient_GivenClientExists_ShouldReturnExistingClient()
        {
            // Arrange
            ITokenProvider tokenProvider = _testServer.Services.GetService<ITokenProvider>();
            ExosTopicClientPool pool = new ExosTopicClientPool(tokenProvider);
            string namespaceName = _configuration["ServiceBus:ActiveConnectionString"];
            string clientName = ExosTopicClientPool.GetClientEntityName(namespaceName, "Topic1");

            // Act
            TopicClient topicClient1 = pool.GetClientEntity(namespaceName, "Topic1");
            TopicClient topicClient2 = pool.GetClientEntity(namespaceName, "Topic1");

            // Assert
            Assert.AreSame(topicClient1, topicClient2);
        }

        [TestMethod]
        public async Task TryCloseTopicClientAsync_GivenTopicClientExists_ShouldCloseAndRemoveClient()
        {
            // Arrange
            ITokenProvider tokenProvider = _testServer.Services.GetService<ITokenProvider>();
            ExosTopicClientPool pool = new ExosTopicClientPool(tokenProvider);
            string namespaceName = _configuration["ServiceBus:ActiveConnectionString"];
            string clientName = ExosTopicClientPool.GetClientEntityName(namespaceName, "Topic1");

            TopicClient topicClient = pool.GetClientEntity(namespaceName, "Topic1");

            // Act
            bool closedAndRemoved = await pool.TryCloseClientEntityAsync(clientName);

            // Assert
            Assert.IsTrue(topicClient.IsClosedOrClosing);
            Assert.IsTrue(closedAndRemoved);
            Assert.IsFalse(pool.ContainsClientEntity(clientName));
        }

        [TestMethod]
        public async Task TryCloseTopicClientAsync_GivenTopicClientNotExists_ShouldCloseAndRemoveClient()
        {
            // Arrange
            ITokenProvider tokenProvider = _testServer.Services.GetService<ITokenProvider>();
            ExosTopicClientPool pool = new ExosTopicClientPool(tokenProvider);
            string namespaceName = _configuration["ServiceBus:ActiveConnectionString"];
            string clientName = ExosTopicClientPool.GetClientEntityName(namespaceName, "Topic1");

            // Act
            bool closedAndRemoved = await pool.TryCloseClientEntityAsync(clientName);

            // Assert
            Assert.IsTrue(closedAndRemoved);
            Assert.IsFalse(pool.ContainsClientEntity(clientName));
        }

        [TestMethod]
        public void GetTopicClient_GivenMultipleThreadsCreatingTopicClient_ShouldCreateOneInstance()
        {
            // Arrange
            ITokenProvider tokenProvider = _testServer.Services.GetService<ITokenProvider>();
            ExosTopicClientPool pool = new ExosTopicClientPool(tokenProvider);
            string namespaceName = _configuration["ServiceBus:ActiveConnectionString"];
            string clientName = ExosTopicClientPool.GetClientEntityName(namespaceName, "Topic1");

            TopicClient client = null;
            Action createClient = () =>
            {
                TopicClient topicClient = pool.GetClientEntity(namespaceName, "Topic1");
                if (client != null)
                {
                    Assert.AreSame(client, topicClient);
                }

                client = topicClient;
            };

            // Act
            Parallel.Invoke(
                createClient, createClient, createClient, createClient, createClient);

            // Assert
            Assert.IsNotNull(client);
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
