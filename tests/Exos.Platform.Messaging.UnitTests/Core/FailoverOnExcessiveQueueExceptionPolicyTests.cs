#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.Messaging.UnitTests.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class FailoverOnExcessiveQueueExceptionPolicyTests
    {
        private const int _extraInvocationCount = 1;
        private const string _activeString = "ActiveString";
        private const string _passiveString = "PassiveString";
        private const string _inScopeExceptionFullName = "Exos.Platform.Messaging.UnitTests.Stubs.TestingInScopeStubException";

        private static FailoverConfig _stubConfig;
        private static MessageEntity _stubEntity;
        private Mock<ExosClientEntityPool<QueueClient>> _mockPool;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            InitializeStubData();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
            _mockPool = GetMockClientPool();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockPool.Reset();
            _mockPool = null;
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenNoException_ShouldNotIncrement()
        {
            // Arrange
            IFailoverPolicy<QueueClient> policy = new FailoverOnExcessiveQueueExceptionPolicy(_stubConfig);

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity);
            });

            // Assert
            Assert.IsFalse(policy.ShouldFailover);
        }

        private static void InitializeStubData()
        {
            _stubConfig = new FailoverConfig
            {
                IsFailoverEnabled = true,
                ExceptionThreshold = 2,
                ExceptionNamesString = _inScopeExceptionFullName,
            };

            _stubEntity = new MessageEntity
            {
                ConnectionString = _activeString,
                PassiveConnectionString = _passiveString,
            };
        }

        private static Mock<ExosClientEntityPool<QueueClient>> GetMockClientPool()
        {
            Mock<ExosClientEntityPool<QueueClient>> mockClientPool = new Mock<ExosClientEntityPool<QueueClient>>();
            mockClientPool
                .Setup(p => p.GetClientEntity(
                    It.Is<string>(_activeString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()))
                .Returns(default(QueueClient));
            mockClientPool
                .Setup(p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()))
                .Returns(default(QueueClient));
            mockClientPool
                .Setup(p => p.TryCloseClientEntityAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);
            return mockClientPool;
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters

