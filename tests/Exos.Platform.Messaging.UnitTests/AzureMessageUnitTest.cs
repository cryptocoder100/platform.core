#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA2201 // Do not raise reserved exception types
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA1305 // Specify IFormatProvider
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
    using AzureMessagingTest.Testmodel;
    using Exos.Platform.AspNetCore.AppConfiguration;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Core.Extension;
    using Exos.Platform.Messaging.Core.Listener;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [ExcludeFromCodeCoverage]

    public class AzureMessageUnitTest
    {
        private static Assembly _currentAssembly;
        private static IConfigurationRoot _configuration;
        private static IConfiguration _messagingSection;
        private static IMessagePublisher _mocMessagePublisher;
        private static IMessagingRepository _mockMessagingRepository;
        private static string _connectionString;
        private static IServiceProvider _serviceProvider;

        [ClassInitialize]

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

            var services = new ServiceCollection()
               .AddLogging()
               .AddTransient<IExosMessaging, ExosMessaging>();

            services.Configure<MessageSection>(_messagingSection);

            services.AddOptions();
            services.ConfigureAzureServiceBusEntityListener(_configuration);
            _serviceProvider = services.BuildServiceProvider();

            InitializeTestData();
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void Validate_Json_Load_And_Check_Url_Exists()
        {
            var connectionString = _messagingSection["MessageDb"];
            Assert.IsNotNull(connectionString);
        }

        [TestMethod]
        public void Insert_MessageLog_data()
        {
            var connectionString = _messagingSection["MessageDb"];
            MessagingDbContext messagingDbContext = new MessagingDbContext(connectionString);
            MessagingRepository repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            MessageLog messagingLog = new MessageLog
            {
                MessageGuid = Guid.NewGuid(),
                Payload = "{\"test\": \"test\"}",
                Publisher = "TestPublisher",
                TransactionId = "ABCD",
                ReceivedDateTime = DateTime.UtcNow,
                CreatedById = 1
            };
            repository.Add(messagingLog);
        }

        [TestMethod]
        public void Get_MessageEntity_data()
        {
            var abc = _mockMessagingRepository.GetAll<MessageEntity>();
            Assert.IsNotNull(abc);
        }

        [TestMethod]
        public void Get_MessageEntity_data_With_DB()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var abc = repository.GetAll<MessageEntity>();
            Assert.IsNotNull(abc);
        }

        [TestMethod]
        public void Get_MessageEntity_By_Owner_EntityName()
        {
            var repository = new MessagingRepository(new MessagingDbContext(_connectionString), _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var messageEntity = repository.GetMessageEntity("Topic1", "MarketPerformance");
            Assert.IsNotNull(messageEntity);
            string output = JsonSerializer.Serialize(messageEntity);
            Assert.IsNotNull(output);
        }

        [TestMethod]
        public void Insert_PublishErrorMessageLog_data()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var errorLog = new PublishErrorMessageLog
            {
                MessageGuid = Guid.NewGuid(),
                Payload = "{\"test\": \"test\"}",
                Publisher = "TestPublisher",
                TransactionId = "ABCD",
                FailedDateTime = DateTime.UtcNow,
                Status = "FAILED",
                Comments = "Testing....",
                CreatedById = 1
            };
            repository.Add(errorLog);
        }

        [TestMethod]
        public void TestTopic()
        {
            MessagingDbContext messagingDbContext = new MessagingDbContext(_connectionString);
            MessagingRepository repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            ExosMessaging m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            // Task.Run(async () =>
            // {
            m.ValidateAndInitializeAzureEntities("Topic1", "MarketPerformance");
            // Actual test code here.
            // }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public async Task Publish_Message_To_Topic_With_No_Filter()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };
            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        public async Task Mock_Publish_Message_To_Topic_With_No_Filter()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _mocMessagePublisher);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };
            List<ExosMessage> messageList = new List<ExosMessage>();
            messageList.Add(message);
            string jsonMessage = JsonSerializer.Serialize(messageList);
            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        [Ignore]
        public async Task Mock_Publish_Message_To_Topic_With_WrongURL_Active_And_Passive_Writing_To_FailedMessage_Table()
        {
            var m = new ExosMessaging(_mockMessagingRepository, _serviceProvider, NullLoggerFactory.Instance);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic2", EntityOwner = "IctTest" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };

            // Act
            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9001, result);
        }

        [TestMethod]
        [Ignore]
        public async Task Publish_Message_To_Topic_With_No_WrongURL_Active_And_Passive_Writing_To_FailedMessage_Table()
        {
            MessagingDbContext messagingDbContext = new MessagingDbContext(_connectionString);
            MessagingRepository repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };
            // Act
            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9001, result);
        }

        [TestMethod]
        public async Task Publish_Message_To_Topic_With_No_WrongURL_Calling_Passive_And_Successful_Publish()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());

            // Arrange
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };
            // Act
            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        public void AuthorizationRequiredException_default_Exception_Constructor()
        {
            const string expectedMessage = "Exception 'ExosMessagingException' received.";
            // Act
            var exosMessagingException = new ExosMessagingException(expectedMessage);
            // Assert
            Assert.IsNull(exosMessagingException.ResourceReferenceProperty);
            Assert.IsNull(exosMessagingException.InnerException);
            Assert.AreEqual(expectedMessage, exosMessagingException.Message);
        }

        [TestMethod]
        [Ignore]
        public async Task TestReadDeadLetterMessages()
        {
            MessagingDbContext messagingDbContext = new MessagingDbContext(_connectionString);
            MessagingRepository repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var messaging = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            // var result = await messaging.ReadDlqMessages("cardupdateanalytics", "Analytics", "testbatch", 10);
            var result = await messaging.ReadDlqMessages("Topic1", "MarketPerformance", "sub", 10).ConfigureAwait(false);
            // var result = await messaging.ReadDlqMessages("subcontractorprofile", "VendorManagement", "SubContractorProfile", 10);
        }

        [TestMethod]
        public void AuthorizationRequiredException_With_Inner_Exception()
        {
            // Arrange
            const string expectedMessage = "Exception!!!";

            var innerExceptiion = new Exception("InnerException!");

            // Act
            var exosMessagingException = new ExosMessagingException(expectedMessage, innerExceptiion);

            // Assert
            Assert.IsNull(exosMessagingException.ResourceReferenceProperty);
            Assert.AreEqual(innerExceptiion, exosMessagingException.InnerException);
            Assert.AreEqual(expectedMessage, exosMessagingException.Message);
        }

        [TestMethod]
        [ExpectedException(typeof(ExosMessagingException), "Message configuration or Data can't be null.")]
        public async Task NullConfiguration_Or_Message_Exception()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            await m.PublishMessageToTopic(new ExosMessage()).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ExosMessagingException), "Message configuration or Data can't be null.")]
        public async Task NullConfigurationException()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            await m.PublishMessageToTopic(null).ConfigureAwait(false);
        }

        [TestMethod]
        public void TestTopicsGrouping()
        {
            // Not related to code ,testing  logic
            IList<ExosMessage> incomingMessages = new List<ExosMessage>();
            string entityName = "test0";
            for (int i = 0; i < 10; i++)
            {
                var message = new ExosMessage
                {
                    Configuration = new MessageConfig { EntityName = entityName, EntityOwner = "IctTest" },
                    MessageData = new MessageData
                    {
                        PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                        Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                    }
                };
                if (i == 5)
                {
                    entityName = "test1";
                }

                incomingMessages.Add(message);
            }

            var distinctTopicsNames = incomingMessages.GroupBy(a => a.Configuration.EntityName)
                .Select(grp => grp.First().Configuration.EntityName)
                .ToList();

            Assert.AreEqual(2, distinctTopicsNames.Count);
            int[] topics = new[] { 0, 0 };
            int index = 0;
            foreach (var v in distinctTopicsNames)
            {
                var distinctTopics = incomingMessages.Where(a => a.Configuration.EntityName == v).ToList();
                topics[index++] = distinctTopics.Count;
            }

            Assert.AreEqual(6, topics[0]);
            Assert.AreEqual(4, topics[1]);
        }

        [TestMethod]

        public async Task Mock_Publish_Multiple_Messages_to_Queue()
        {
            // Not related to code ,testing  logic
            IList<ExosMessage> incomingMessages = new List<ExosMessage>();
            string entityName = "Topic1";
            for (int i = 0; i < 10; i++)
            {
                var message = new ExosMessage
                {
                    Configuration = new MessageConfig { EntityName = entityName, EntityOwner = "MarketPerformance" },
                    MessageData = new MessageData
                    {
                        PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                        Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                    }
                };
                if (i == 5)
                {
                    entityName = "Topic2";
                }

                incomingMessages.Add(message);
            }

            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _mocMessagePublisher);
            var result = await m.PublishMessageToTopic(null, incomingMessages).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        public async Task Test_Dependancy_Injection_test()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddOptions()
                .AddTransient<IExosMessaging, ExosMessaging>()
                .AddSingleton<IConfiguration>(_configuration);

            services.Configure<MessageSection>(_configuration.GetSection("Messaging"));
            var exosMessaging = services.BuildServiceProvider().GetService<IExosMessaging>();

            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}",
                    AdditionalMetaData = new List<MessageMetaData>
                    {
                        new MessageMetaData { DataFieldName = "Priority", DataFieldValue = "High" },
                        new MessageMetaData { DataFieldName = "Order", DataFieldValue = "123" }
                    }
                }
            };

            var returnValue = await exosMessaging.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9999, returnValue);
        }

        [TestMethod]
        public async Task Test_Delayed_Publish_To_Queue()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}",
                    AdditionalMetaData = new List<MessageMetaData>
                    {
                        new MessageMetaData { DataFieldName = "Priority", DataFieldValue = "High" },
                        new MessageMetaData { DataFieldName = "Order", DataFieldValue = "123" },
                        new MessageMetaData { DataFieldName = MessageMetaData.ScheduleEnqueueDelayTime, DataFieldValue = "60" }
                    }
                }
            };
            int result = await m.PublishMessageToTopic(message).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        [Ignore]
        public async Task Test_Dependancy_Injection_test_Listener()
        {
            MessageProcessor processor = new TestMessageProcessor();
            var services = new ServiceCollection()
                    .AddLogging()
                    .AddTransient<IExosMessageListener, ExosMessageListener>();
            services.Configure<MessageSection>(_configuration.GetSection("Messaging"));
            var exosMessageListener = services.BuildServiceProvider().GetService<IExosMessageListener>();
            var consumer = exosMessageListener.RegisterServiceBusEntityListener(
                 new MessageListenerConfig { EntityName = "Topic1", EntityOwner = "IctTest", RetryCount = 5, SubscriptionName = "AllMessages" }, processor);

            // SEND a message to same topic

            await Publish_Message_To_Topic_With_No_Filter().ConfigureAwait(false);
            await Task.Delay(5000).ConfigureAwait(false);
            // await consumer.Close().ConfigureAwait(false);

            // Console.ReadLine();
        }

        [TestMethod]
        public async Task Publish_Message_To_ServiceBus_Queue()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Queue1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };
            int result = await m.PublishMessageToQueue(message).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        public async Task Publish_Message_To_ServiceBus_topic_Order()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" };
            IList<ExosMessage> messages = new List<ExosMessage>();
            for (int i = 0; i < 100; i++)
            {
                var message = new ExosMessage
                {
                    Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                    MessageData = new MessageData
                    {
                        PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                        Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}",
                        AdditionalMetaData = new List<MessageMetaData>
                        {
                            new MessageMetaData { DataFieldName = "OrderBy", DataFieldValue = "True" },
                            new MessageMetaData { DataFieldName = "Date", DataFieldValue = DateTime.Now.ToString() },
                            new MessageMetaData { DataFieldName = "TimeToLive", DataFieldValue = "1" }
                        }
                    }
                };
                messages.Add(message);
            }

            int result = await m.PublishMessageToTopic(configuration, messages).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        public async Task Publish_List_of_Messages_To_ServiceBus_Queue()
        {
            var messagingDbContext = new MessagingDbContext(_connectionString);
            var repository = new MessagingRepository(messagingDbContext, _configuration, NullLoggerFactory.Instance.CreateLogger<MessagingRepository>());
            var m = new ExosMessaging(repository, _serviceProvider, NullLoggerFactory.Instance);
            var message1 = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Queue1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };
            var messag2 = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = "Topic1", EntityOwner = "MarketPerformance" },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = Guid.NewGuid().ToString(),
                    Payload = "{\"Color\": \"Red\",\"Date\":\"" + DateTime.Now + "\"}"
                }
            };

            int result = await m.PublishMessage(new List<ExosMessage> { message1, messag2 }).ConfigureAwait(false);
            Assert.AreEqual(9999, result);
        }

        [TestMethod]
        public void Load_Messaging_Configuration_From_AppSettings()
        {
            var messaging = _configuration.GetSection("Messaging");
            Assert.AreNotEqual(messaging["Listeners:0:EntityName"], string.Empty);
            Assert.AreNotEqual(messaging["Listeners:1:EntityName"], string.Empty);
            MessageProcessor processor = new TestMessageProcessor();
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IExosMessageListener, ExosMessageListener>();
            services.AddOptions();
            services.Configure<MessageSection>(_configuration.GetSection("Messaging"));

            var sp = services.BuildServiceProvider();
            var messageSection = sp.GetService<IOptions<MessageSection>>().Value;
            Assert.IsNotNull(messageSection);
            Assert.AreNotEqual(messageSection.Listeners.FirstOrDefault()?.EntityName, string.Empty);
        }

        [TestMethod]
        public void Load_Messaging_Configuration_From_AppSettings_ServiceExtension()
        {
            var messaging = _configuration.GetSection("Messaging");
            Assert.AreNotEqual(messaging["Listeners:0:EntityName"], string.Empty);
            Assert.AreNotEqual(messaging["Listeners:1:EntityName"], string.Empty);

            // MessageProcessor processor = new TestMessageProcessor();
            var services = new ServiceCollection()
                .AddLogging();
            // .AddSingleton<IExosMessageListener, IExosMessageListener>();
            services.AddOptions();
            services.ConfigureAzureServiceBusEntityListener(_configuration);

            var sp = services.BuildServiceProvider();
            var messageSection = sp.GetService<IOptions<MessageSection>>().Value;
            Assert.IsNotNull(messageSection);
            Assert.AreNotEqual(messageSection.Listeners.FirstOrDefault()?.EntityName, string.Empty);
        }

        [TestMethod]
        public void InitializeType_From_String()
        {
            var messaging = _configuration.GetSection("Messaging");
            var processorName = messaging["Listeners:0:Processor"];
            Assert.AreNotEqual(processorName, string.Empty);
            Type type = Type.GetType(processorName, false, false);
            Assert.IsNotNull(type);
        }

        [TestMethod]
        public void InitializeType_From_String_And_Instantiate()
        {
            var messaging = _configuration.GetSection("Messaging");
            var processorName = messaging["Listeners:0:Processor"];
            Assert.AreNotEqual(processorName, string.Empty);
            Type type = Type.GetType(processorName, false, false);
        }

        // public void TestConfig()
        // {
        //    var webHostBuilder = new WebHostBuilder();
        //    //webHostBuilder.ConfigureServices(
        //    //    s => s.AddSingleton < IStartupConfigurationService, TestStartupConfigurationService <[DBCONTEXT_TYPE] >> ());
        //    webHostBuilder.UseStartup<StartupTest>().ConfigureFileLogging();
        //    var testServer = new TestServer(webHostBuilder);

        // ExosMessaging em = new ExosMessaging();
        //    var client = testServer.CreateClient();

        // var serviceProvider = new ServiceCollection()
        //        .AddLogging()
        //        .AddSingleton<IMessagingRepository, MessagingRepository>()
        //        .BuildServiceProvider();
        // }

        private static void InitializeTestData()
        {
            // Load TestData
            _connectionString = _messagingSection["MessageDb"];
            var result = JsonSerializer.Deserialize<TestData>(File.ReadAllText(@"TestData\testdata.json"));
            Mock<IMessagingRepository> mockMessagingRepo = new Mock<IMessagingRepository>();
            mockMessagingRepo.Setup(mr => mr.GetAll<MessageEntity>()).Returns(result.MessageEntities);
            mockMessagingRepo.Setup(mr => mr.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>())).Returns((string entity, string owner) => (IList<MessageEntity>)result.MessageEntities.Where(x => x.EntityName == entity && x.Owner == owner).ToList());
            _mockMessagingRepository = mockMessagingRepo.Object;
            Mock<IMessagePublisher> mockPublisher = new Mock<IMessagePublisher>();
            mockPublisher.Setup(mr => mr.WriteToTopic(It.IsAny<MessageEntity>(), It.IsAny<IList<Message>>()))
                .Returns(Task.FromResult(9999));
            _mocMessagePublisher = mockPublisher.Object;
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestMessageProcessor : MessageProcessor
    {
        public override Task<bool> Execute(string messageUtfText, MessageProperty messageProperty)
        {
            return Task.FromResult(true);
        }
    }
}
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore CA1305 // Specify IFormatProvider
#pragma warning restore CA1801 // Review unused parameters