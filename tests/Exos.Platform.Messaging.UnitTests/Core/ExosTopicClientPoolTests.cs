namespace Exos.Platform.Messaging.UnitTests.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ExosTopicClientPoolTests
    {
        [TestMethod]
        public void GetRetryPolicy_GivenRetryCountNotDefault_ShouldReturnRetryPolicy()
        {
            // Arrange
            // Act
            var policy = ExosTopicClientPool.GetRetryPolicy(5);

            // Assert
            Assert.IsNotNull(policy);
        }

        [TestMethod]
        [DataRow("", "", ":")]
        [DataRow("test", "Test", "test:Test")]
        [DataRow(
            "Endpoint=sb://__this_is_not_a_name__eus2-01.servicebus.windows.net/;SharedAccessKeyName=default_application_rule;SharedAccessKey=__this_is_not_a_secret__",
            "Test",
            "Endpoint=sb://__this_is_not_a_name__eus2-01.servicebus.windows.net/;SharedAccessKeyName=default_application_rule;SharedAccessKey=__this_is_not_a_secret__:Test")]
        public void GetTopicClientName_GivenInput_ShouldReturnTopicClientKeyName(
            string namespaceName, string entityName, string expectedName)
        {
            // Arrange
            string clientName = ExosTopicClientPool.GetClientEntityName(namespaceName, entityName);

            // Act
            // Assert
            Assert.IsTrue(string.Equals(expectedName, clientName, StringComparison.Ordinal));
        }

        [TestMethod]
        public void ContainsTopicClient_GivenTopicClientName_ShouldDetermineIfNotExists()
        {
            // Arrange
            ExosTopicClientPool pool = new ExosTopicClientPool(null);

            // Act
            // Assert
            Action assert = () => Assert.IsFalse(pool.ContainsClientEntity("_not_exist_"));
            Parallel.Invoke(
                assert, assert, assert, assert, assert, assert, assert, assert, assert);
        }

        [TestMethod]
        public void FailoverConfig_GivenNoExceptionOverwriteFromConfiguration_ShouldReturnDefault()
        {
            // 3 exception names are predefined
            MessageSection section = new MessageSection();
            Assert.AreEqual(3, section.FailoverConfig.ExceptionNames.Count);
        }
    }
}
