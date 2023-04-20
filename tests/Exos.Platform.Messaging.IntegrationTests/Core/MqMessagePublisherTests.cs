#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1801 // Review unused parameters

namespace Exos.Platform.Messaging.IntegrationTests.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.AppConfiguration;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Helper;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class MqMessagePublisherTests
    {
        private const string _invalidActiveConnectionString = "Endpoint=sb://invalid-active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=invalid_rule;SharedAccessKey=invalid_key";
        private const string _invalidPassiveConnectionString = "Endpoint=sb://invalid-passive-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=invalid_rule;SharedAccessKey=invalid_key";
        private static TestServer _testServer;
        private static JObject _testMessages;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var testData = File.ReadAllText("./TestData/messages.json");
            _testMessages = JObject.Parse(testData);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Publisher.json");

            IConfiguration configuration = builder.Build();

            ExosAzureConfigurationResolutionProcessor
                .ProcessTokenResolution(configuration, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            _testServer = new TestServer(new WebHostBuilder()
                .UsePlatformConfigurationDefaults()
                .UsePlatformLoggingDefaults()
                .UseConfiguration(configuration)
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
        public async Task PublishMessageToTopic_GivenMessage_ShouldSucceed()
        {
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            ExosMessage message = _testMessages["happy_path_success"]["message"].ToObject<ExosMessage>();

            // Message should be published successfully to primary namespace
            int responseCode = await m.PublishMessageToTopic(message);
            Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
        }

        [TestMethod]
        public async Task PublishMessageToTopic_GivenFailureOnPrimary_ShouldFallbackToSecondary()
        {
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            IConfiguration config = _testServer.Services.GetService<IConfiguration>();
            ExosMessage message = _testMessages["failure_on_primary"]["message"].ToObject<ExosMessage>();

            // Update active connection string to invalid namespace to raise ServiceBusCommunicationException
            config["ServiceBus:ActiveConnectionString"] = _invalidActiveConnectionString;

            // Message should be published successfully to secondary namespace
            int responseCode = await m.PublishMessageToTopic(message);
            Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
        }

        [TestMethod]
        public async Task PublishMessageToTopic_GivenFailureOnSecondary_ShouldLogToDb()
        {
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            IConfiguration config = _testServer.Services.GetService<IConfiguration>();
            ExosMessage message = _testMessages["failure_on_primary_and_secondary"]["message"].ToObject<ExosMessage>();

            // Update active/passive connection string to invalid namespace to raise ServiceBusCommunicationException
            config["ServiceBus:ActiveConnectionString"] = _invalidActiveConnectionString;
            config["ServiceBus:PassiveConnectionString"] = _invalidPassiveConnectionString;

            // Message publish should fail and should be logged into db
            int responseCode = await m.PublishMessageToTopic(message);
            Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
        }

        [TestMethod]
        public async Task PublishMessageToTopic_GivenFailureOnPrimaryAndExceptionInScope_ShouldFallbackToSecondary()
        {
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            IConfiguration config = _testServer.Services.GetService<IConfiguration>();
            ExosMessage message = _testMessages["failure_on_primary"]["message"].ToObject<ExosMessage>();

            // Update active connection string to invalid namespace to raise ServiceBusCommunicationException
            config["ServiceBus:ActiveConnectionString"] = _invalidActiveConnectionString;

            // All messages should be published successfully to secondary namespace
            // Switch should not be turned on to failover to secondary namespace
            List<Task<int>> tasks = new List<Task<int>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(m.PublishMessageToTopic(message));
            }

            await Task.WhenAll(tasks);
            tasks.ForEach(async task =>
            {
                int responseCode = await task;
                Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
            });
        }

        [TestMethod]
        public async Task PublishMessageToTopic_GivenFailureOnPrimaryAndExceedsThreshold_ShouldFailoverToSecondary()
        {
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            IConfiguration config = _testServer.Services.GetService<IConfiguration>();
            MessageSection section = _testServer.Services.GetService<IOptions<MessageSection>>().Value;
            ExosMessage message = _testMessages["failure_on_primary"]["message"].ToObject<ExosMessage>();

            // Update exception threshold to 1 to trigger switching
            section.FailoverConfig.ExceptionThreshold = 1;
            // Update active connection string to invalid namespace to raise ServiceBusCommunicationException
            config["ServiceBus:ActiveConnectionString"] = _invalidActiveConnectionString;

            // First 2 messages should be published successfully to secondary namespace
            // Switch should be turned on to failover to secondary namespace
            // Rest messages should be published successfully to secondary namespace
            List<Task<int>> tasks = new List<Task<int>>();
            for (int i = 0; i < 5; i++)
            {
                message.MessageData.Payload += $" {i}";
                tasks.Add(m.PublishMessageToTopic(message));
            }

            await Task.WhenAll(tasks);
            tasks.ForEach(async task =>
            {
                int responseCode = await task;
                Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
            });
        }

        [TestMethod]
        public async Task PublishMessageToTopic_GivenFailureWhenSecondaryAsActivePrimary_ShouldLogToDb()
        {
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            IConfiguration config = _testServer.Services.GetService<IConfiguration>();
            MessageSection section = _testServer.Services.GetService<IOptions<MessageSection>>().Value;
            ExosMessage message = _testMessages["failure_on_primary_and_secondary"]["message"].ToObject<ExosMessage>();

            // Update exception threshold to 1 to trigger switching
            section.FailoverConfig.ExceptionThreshold = 1;
            // Update active connection string to invalid namespace to raise ServiceBusCommunicationException
            config["ServiceBus:ActiveConnectionString"] = _invalidActiveConnectionString;
            config["ServiceBus:PassiveConnectionString"] = _invalidPassiveConnectionString;

            // First message publish should fail and should be logged into db
            // Switch should be turned on to failover to secondary namespace
            // Rest messages should also fail and should be logged into db
            List<Task<int>> tasks = new List<Task<int>>();
            for (int i = 0; i < 5; i++)
            {
                message.MessageData.Payload += $" {i}";
                tasks.Add(m.PublishMessageToTopic(message));
            }

            await Task.WhenAll(tasks);
            tasks.ForEach(async task =>
            {
                int responseCode = await task;
                Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
            });
        }

        [TestMethod]
        public async Task PublishMessageToTopic_GivenFailureOnPrimaryAndNotExceedsThresholdInSlidingDuration_ShouldFallbackToSecondary()
        {
            MessageSection section = _testServer.Services.GetService<IOptions<MessageSection>>().Value;
            // Update exception threshold to 2 to trigger switching
            section.FailoverConfig.ExceptionThreshold = 2;
            section.FailoverConfig.SlidingDurationInSeconds = 5;

            IConfiguration config = _testServer.Services.GetService<IConfiguration>();
            IExosMessaging m = _testServer.Services.GetService<IExosMessaging>();
            ExosMessage message = _testMessages["failure_on_primary"]["message"].ToObject<ExosMessage>();

            // Update active connection string to invalid namespace to raise ServiceBusCommunicationException
            config["ServiceBus:ActiveConnectionString"] = _invalidActiveConnectionString;

            // Message should be published successfully to secondary namespace
            // Switch should be off to failover to secondary namespace
            int responseCode = await m.PublishMessageToTopic(message);
            Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);

            Thread.Sleep(5 * 1000);

            // Message should be published successfully to secondary namespace
            // Switch should be off to failover to secondary namespace since exception count doesn't exceed threshold in sliding duration
            responseCode = await m.PublishMessageToTopic(message);
            Assert.AreEqual(ExosMessagingConstant.SuccessCode, responseCode);
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
