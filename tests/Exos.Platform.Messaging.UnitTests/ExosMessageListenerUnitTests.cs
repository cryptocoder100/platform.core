#pragma warning disable CA1801 // Review unused parameters

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Exos.Platform.Messaging.Core;
using Exos.Platform.Messaging.Core.Listener;
using Exos.Platform.Messaging.Helper;
using Exos.Platform.Messaging.Repository;
using Exos.Platform.Messaging.Repository.Model;
using Exos.Platform.Messaging.UnitTests.Core;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace Exos.Platform.Messaging.UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ExosMessageListenerUnitTests
    {
        private static IExosMessageListener _listener;
        private static Mock<IOptions<MessageSection>> _mockOptions;
        private static Mock<MessageProcessor> _mockProcessor;
        private static Mock<IMessagingRepository> _mockRepository;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _mockRepository = new Mock<IMessagingRepository>();
            _mockOptions = new Mock<IOptions<MessageSection>>();
            _mockProcessor = new Mock<MessageProcessor>();

            var mockData = JObject.Parse(File.ReadAllText(@"TestData\MockListenerTestData.json"));
            var mockEntities = mockData["MessageEntities"].ToObject<List<MessageEntity>>();
            var mockConfigs = mockData["Listeners"].ToObject<List<MessageListenerConfig>>();

            _mockRepository.Setup(r => r.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>())).Returns((string entityName, string owner) => mockEntities.Where(m => m.EntityName == entityName && m.Owner == owner).ToList());
            _mockRepository.Setup(r => r.GetAll<MessageEntity>()).Returns(mockEntities);
            _mockRepository.Setup(r => r.Connection.ConnectionString).Returns("mock connection string");
            _mockOptions.Setup(o => o.Value).Returns(new MessageSection()
            {
                Environment = "Dev",
                FailoverConfig = new FailoverConfig
                {
                    IsFailoverEnabled = false,
                },
                Listeners = mockConfigs
            });
            _mockProcessor.Setup(p => p.Execute(It.IsAny<string>(), It.IsAny<MessageProperty>())).ReturnsAsync(true);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Create listener object for each test so tests are decoupled
            _listener = new ExosMessageListener(_mockOptions.Object, NullLoggerFactory.Instance, _mockRepository.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockRepository.Invocations.Clear();
        }

        [TestMethod]
        [ExpectedException(typeof(ExosMessagingException))]
        public void RegisterServiceBusEntityListener_NullConfig_ShouldThrowException()
        {
            var consumers = _listener.RegisterServiceBusEntityListener(null, null);
        }

        [TestMethod]
        [DataRow(false, 1)]
        [DataRow(true, 2)]
        public void RegisterServiceBusEntityListener_TopicListener_ShouldReturnConsumers(bool isFailoverEnabled, int expectedConsumersCount)
        {
            _mockOptions.Object.Value.FailoverConfig.IsFailoverEnabled = isFailoverEnabled;

            var consumers = _listener.RegisterServiceBusEntityListener(_mockOptions.Object.Value.Listeners[0], _mockProcessor.Object);

            Assert.IsNotNull(consumers);
            Assert.AreEqual(expectedConsumersCount, consumers.Count);
        }

        [TestMethod]
        public void RegisterServiceBusEntityListener_TopicListenerSameNamespace_ShouldReturnOneConsumer()
        {
            _mockOptions.Object.Value.FailoverConfig.IsFailoverEnabled = true;

            var consumers = _listener.RegisterServiceBusEntityListener(_mockOptions.Object.Value.Listeners[4], _mockProcessor.Object);

            Assert.IsNotNull(consumers);
            Assert.AreEqual(1, consumers.Count);
        }

        [TestMethod]
        [DataRow(false, 1)]
        [DataRow(true, 2)]
        public void RegisterServiceBusEntityListener_QueueListener_ShouldReturnConsumers(bool isFailoverEnabled, int expectedConsumersCount)
        {
            _mockOptions.Object.Value.FailoverConfig.IsFailoverEnabled = isFailoverEnabled;

            var consumers = _listener.RegisterServiceBusEntityListener(_mockOptions.Object.Value.Listeners[1], _mockProcessor.Object);

            Assert.IsNotNull(consumers);
            Assert.AreEqual(expectedConsumersCount, consumers.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ExosMessagingException))]
        public void RegisterServiceBusEntityListener_TopicOrQueueMessageEntityNotFound_ShouldThrowException()
        {
            var consumers = _listener.RegisterServiceBusEntityListener(_mockOptions.Object.Value.Listeners[2], _mockProcessor.Object);
        }

        [TestMethod]
        public void StartListener_TopicListenersWithDisabledFlag_ShouldStartEnabledListener()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Start only enabled topic listeners
            _listener.StartListener(mockServiceProvider.Object);

            Assert.AreEqual(5, _mockOptions.Object.Value.Listeners.Count);
            _mockRepository.Verify(r => r.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void StartAllEntityListeners_MessageEntityWithInactiveStatus_ShouldStartActiveListener()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Start only active entity listeners
            _listener.StartAllEntityListeners(mockServiceProvider.Object);

            _mockRepository.Verify(r => r.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void StartEntityListener_ListenerConfigExists_ShouldStartListener()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Start specifed listener
            _listener.StartEntityListener(mockServiceProvider.Object, "mock_entity_1", "mock_subscription_1");

            _mockRepository.Verify(r => r.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartEntityListener_ListenerAlreadyStarted_ShouldSkip()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Start listener
            _listener.StartListener(mockServiceProvider.Object);
            _mockRepository.Verify(r => r.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _mockRepository.Invocations.Clear();

            // Start specifed listener
            _listener.StartEntityListener(mockServiceProvider.Object, "mock_entity_1", "mock_subscription_1");
            _mockRepository.Verify(r => r.GetMessageEntity(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void GetActiveEntityListeners_ShouldReturnListenerNames()
        {
            var listeners = _listener.GetActiveEntityListeners();
            Assert.AreEqual(0, listeners.Count);

            var mockServiceProvider = new Mock<IServiceProvider>();
            _listener.StartListener(mockServiceProvider.Object);

            listeners = _listener.GetActiveEntityListeners();
            Assert.AreEqual(2, listeners.Count);
        }

        [TestMethod]
        [Ignore]
        public async Task StopListener_StopAll_ShouldStopListeners()
        {
            // Start listeners
            var mockServiceProvider = new Mock<IServiceProvider>();
            _listener.StartListener(mockServiceProvider.Object);

            // Stop listeners
            await _listener.StopListener();

            var listeners = _listener.GetActiveEntityListeners();
            Assert.AreEqual(0, listeners.Count);
        }

        [TestMethod]
        [Ignore]
        public async Task StopListener_StopGivenListener_ShouldStopListener()
        {
            // Start listeners
            var mockServiceProvider = new Mock<IServiceProvider>();
            _listener.StartListener(mockServiceProvider.Object);

            // Stop given listener
            await _listener.StopListener("mock_entity_1", "mock_subscription_1");

            var listeners = _listener.GetActiveEntityListeners();
            Assert.AreEqual(0, listeners.Count);
        }

        [TestMethod]
        public async Task MessageConsumer_ExceptionReceivedHandler_LogsException()
        {
            Func<object, Type, bool> state = (v, t) => true;
            var repository = new Mock<IMessagingRepository>();
            var logger = new Mock<ILogger>();

            var consumer = new FakeMessageConsumer(repository.Object, string.Empty, logger.Object);
            await consumer.PublicExceptionReceivedHandler(new ExceptionReceivedEventArgs(
                new IOException(),
                "Received",
                "sb-app-dev-eus2-01.servicebus.windows.net",
                "IN_WO_CANCEL/Subscriptions/StopOrderGeneratedSubscription",
                "123"));

            await consumer.PublicExceptionReceivedHandler(new ExceptionReceivedEventArgs(
                null,
                "Received",
                "sb-app-dev-eus2-01.servicebus.windows.net",
                "IN_WO_CANCEL/Subscriptions/StopOrderGeneratedSubscription",
                "123"));

            // https://adamstorr.azurewebsites.net/blog/mocking-ilogger-with-moq
            logger.Verify(
                logger =>
                logger.Log(
                    It.Is<LogLevel>(ll => ll == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => state(v, t)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Exactly(2));
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
