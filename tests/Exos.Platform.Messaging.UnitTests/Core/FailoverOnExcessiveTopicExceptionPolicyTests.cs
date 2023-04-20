#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.Messaging.UnitTests.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Repository.Model;
    using Exos.Platform.Messaging.UnitTests.Stubs;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class FailoverOnExcessiveTopicExceptionPolicyTests
    {
        private const int _extraInvocationCount = 1;
        private const string _activeString = "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_";
        private const string _activeString1 = "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_";
        private const string _activeString2 = "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule_too;SharedAccessKey=_not_a_key_either_";
        private const string _passiveString = "Endpoint=sb://passive-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_";
        private const string _inScopeExceptionFullName = "Exos.Platform.Messaging.UnitTests.Stubs.TestingInScopeStubException";

        private static FailoverConfig _stubConfig;
        private static MessageEntity _stubEntity;
        private Mock<ExosClientEntityPool<TopicClient>> _mockPool;

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
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity);
            });

            // Assert
            Assert.IsFalse(policy.ShouldFailover);
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenFlagIsOff_ShouldOnlyUseThePrimaryNamespace()
        {
            // Arrange
            Exception ex = new TestingInScopeStubException();
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(new FailoverConfig
            {
                IsFailoverEnabled = false,
                ExceptionThreshold = 2,
                ExceptionNamesString = _inScopeExceptionFullName,
            });

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity);
            });

            // Assert
            Assert.IsFalse(policy.ShouldFailover);
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_activeString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(2 * (_stubConfig.ExceptionThreshold + _extraInvocationCount)));
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(0));
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenFlagIsOnButSameNamespaces_ShouldOnlyUseThePrimaryNamespace()
        {
            // Arrange
            Exception ex = new TestingInScopeStubException();
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            var entity = new MessageEntity
            {
                ConnectionString = _activeString1,
                PassiveConnectionString = _activeString2,
            };

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, entity, true, ex);
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, entity);
            });

            // Assert
            Assert.IsTrue(policy.ShouldFailover);
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_activeString1, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(2 * (_stubConfig.ExceptionThreshold + _extraInvocationCount)));
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_activeString2, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(0));
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenExceptionNotInScopePrimaryHealthy_ShouldPass()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex = new TestingOutOfScopeStubException();

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
            });

            // Assert
            Assert.IsFalse(policy.ShouldFailover);
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenExceptionInScopePrimaryHealthy_ShouldIncrementCounter()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex = new TestingInScopeStubException();

            // Act
            Parallel.For(1, _stubConfig.ExceptionThreshold, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
            });

            // Assert
            Assert.IsFalse(policy.ShouldFailover);
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenThresholdReached_ShouldSwitchFlag()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex = new TestingInScopeStubException();

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
            });

            // Assert
            Assert.IsTrue(policy.ShouldFailover);
            _mockPool.Verify(
                p => p.TryCloseClientEntityAsync(It.IsAny<string>()),
                Times.Never());
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(_stubConfig.ExceptionThreshold));
        }

        [TestMethod]
        public async Task EnsureExecutionFailoverAsync_GivenThresholdReached_ShouldSwitchTopicClientOnNextInvocation()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex = new TestingInScopeStubException();
            Parallel.For(0, _stubConfig.ExceptionThreshold, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
            });

            // Act
            await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity);

            // Assert
            Assert.IsTrue(policy.ShouldFailover);
            _mockPool.Verify(
                p => p.TryCloseClientEntityAsync(It.IsAny<string>()),
                Times.Once());
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(_stubConfig.ExceptionThreshold + 1));
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenThresholdReached_ShouldBeThreadSafe()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex1 = new TestingInScopeStubException();

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex1);
            });

            // Assert
            int iteration = 10;
            Parallel.For(0, iteration, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity);
                Assert.IsTrue(policy.ShouldFailover);
            });
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(_stubConfig.ExceptionThreshold + iteration));
        }

        [TestMethod]
        public void EnsureExecutionFailoverAsync_GivenConcurrentRetryAfterThresholdReached_ShouldUseTheActivePrimary()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex = new TestingInScopeStubException();

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
            });

            // Assert
            Assert.IsTrue(policy.ShouldFailover);
            _mockPool.Verify(
                p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()),
                Times.Exactly(_stubConfig.ExceptionThreshold + _extraInvocationCount));
        }

        [TestMethod]
        public async Task EnsureExecutionFailoverAsync_GivenRetryAfterThresholdReached_ShouldEventuallyCloseTheExceptionalPrimary()
        {
            // Arrange
            IFailoverPolicy<TopicClient> policy = new FailoverOnExcessiveTopicExceptionPolicy(_stubConfig);
            Exception ex = new TestingInScopeStubException();

            // Act
            Parallel.For(0, _stubConfig.ExceptionThreshold + _extraInvocationCount, async (i) =>
            {
                await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);
            });

            await policy.EnsureExecutionFailoverAsync(_mockPool.Object, _stubEntity, true, ex);

            // Assert
            Assert.IsTrue(policy.ShouldFailover);
            // the concurrent threads may or may not close the exceptional primary, but the next invocation
            // will certainly close it.
            _mockPool.Verify(
                p => p.TryCloseClientEntityAsync(It.IsAny<string>()),
                _extraInvocationCount > 0 ? Times.AtLeastOnce() : Times.AtMost(1 + _extraInvocationCount));
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

        private static Mock<ExosClientEntityPool<TopicClient>> GetMockClientPool()
        {
            Mock<ExosClientEntityPool<TopicClient>> mockClientPool = new Mock<ExosClientEntityPool<TopicClient>>();
            mockClientPool
                .Setup(p => p.GetClientEntity(
                    It.Is<string>(_activeString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()))
                .Returns(default(TopicClient));
            mockClientPool
                .Setup(p => p.GetClientEntity(
                    It.Is<string>(_passiveString, StringComparer.Ordinal),
                    It.IsAny<string>(),
                    It.IsAny<RetryPolicy>()))
                .Returns(default(TopicClient));
            mockClientPool
                .Setup(p => p.TryCloseClientEntityAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);
            return mockClientPool;
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
