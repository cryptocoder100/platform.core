#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA1801 // Review unused parameters

namespace Exos.Platform.Messaging.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.AppConfiguration;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Core.Extension;
    using Exos.Platform.Messaging.Repository;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class FailedMessagesUnitTest
    {
        private static Assembly _currentAssembly;
        private static string _connectionString;
        private static IConfigurationRoot _configuration;
        private static IConfiguration _messagingSection;
        private static IServiceProvider _serviceProvider;

        [ClassInitialize]
        [Obsolete(" ExosAzureConfigurationResolutionProcessor.ProcessTokenResolution is obsolete")]
        public static void MyClassInitialize(TestContext testContext)
        {
            _currentAssembly = Assembly.GetExecutingAssembly();

            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json")
                .Build();

            ExosAzureConfigurationResolutionProcessor
                .ProcessTokenResolution(_configuration, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            _messagingSection = _configuration.GetSection("Messaging");
            _connectionString = _messagingSection["MessageDb"];
            var services = new ServiceCollection()
               .AddLogging()
               .AddTransient<IExosMessaging, ExosMessaging>();

            services.Configure<MessageSection>(_messagingSection);

            services.AddOptions();
            services.ConfigureAzureServiceBusEntityListener(_configuration);
            _serviceProvider = services.BuildServiceProvider();
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public async Task GetFailedMessagesByTransactions()
        {
            var result = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(@"TestData\FailedMessagesTransactions.json").ConfigureAwait(false));

            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var failedMessageService = new FailedMessageService(repository);
            var failedMessages = await failedMessageService.GetFailedMessagesByTransactionIds(result).ConfigureAwait(false);

            Assert.IsTrue(failedMessages != null);
            // Assert.IsTrue(failedMessages != null && failedMessages.Any());
        }

        [TestMethod]
        public async Task GetFailedMessagesByFailedDate()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var failedMessageService = new FailedMessageService(repository);
            var endDate = DateTime.Now;
            var startDate = endDate.AddHours(-1);
            var failedMessages = await failedMessageService.GetFailedMessagesByFailedDate(startDate, endDate).ConfigureAwait(false);

            Assert.IsTrue(failedMessages != null && failedMessages.Any());
        }

        [TestMethod]
        public async Task GetFailedMessagesByIds()
        {
            var result = JsonSerializer.Deserialize<long[]>(await File.ReadAllTextAsync(@"TestData\FailedMessagesIds.json").ConfigureAwait(false));

            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var failedMessageService = new FailedMessageService(repository);
            var failedMessages = await failedMessageService.GetFailedMessagesByIds(result).ConfigureAwait(false);

            Assert.IsTrue(failedMessages != null);
            // Assert.IsTrue(failedMessages != null && failedMessages.Count() == result.Length);
        }

        [TestMethod]
        public async Task UpdateFailedMessagesStatus()
        {
            var result = JsonSerializer.Deserialize<int[]>(await File.ReadAllTextAsync(@"TestData\FailedMessagesIds.json").ConfigureAwait(false));

            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var failedMessageService = new FailedMessageService(repository);
            var rowsAffected = await failedMessageService.UpdateFailedMessageStatus(result[0], "SUCCEEDED").ConfigureAwait(false);

            // Assert.IsTrue(rowsAffected == 1);
        }

        [TestMethod]
        public async Task RetryFailedMessageProcessorThroughSBTest()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            string testPayload = await File.ReadAllTextAsync(@"TestData\Payload.json").ConfigureAwait(false);

            ExosMessage message = new ExosMessage()
            {
                Configuration = new MessageConfig { EntityName = "RETRY_FAILED_MESSAGE", EntityOwner = "RetryFailedMessageSvc" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = testPayload,
                    AdditionalMetaData = new List<MessageMetaData>
                    {
                        new MessageMetaData { DataFieldName = "EventName", DataFieldValue = "RETRY_FAILED_MESSAGE" },
                        new MessageMetaData { DataFieldName = "EnitityName", DataFieldValue = "RetryMessage" },
                        new MessageMetaData { DataFieldName = "Priority", DataFieldValue = "0" }
                    }
                }
            };

            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }
    }
}
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore CA1801 // Review unused parameters