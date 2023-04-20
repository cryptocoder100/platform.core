#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.Messaging.UnitTests.Helper
{
    using System.Diagnostics.CodeAnalysis;
    using Exos.Platform.Messaging.Helper;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class MessagingHelperTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
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
        [DataRow(null, null, true)]
        [DataRow("", "", true)]
        [DataRow(null, "", false)]
        [DataRow(" ", "", false)]
        [DataRow(" ;", ";", false)]
        [DataRow(
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_",
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_",
            true)]
        [DataRow(
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_",
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_either_",
            true)]
        [DataRow(
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_",
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule_too;SharedAccessKey=_not_a_key_",
            true)]
        [DataRow(
            "Endpoint=sb://active-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_",
            "Endpoint=sb://active1-sb-namespace.servicebus.windows.net/;SharedAccessKeyName=rule;SharedAccessKey=_not_a_key_",
            false)]
        [DataRow(
            "sb://active-sb-namespace.servicebus.windows.net/",
            "sb://active1-sb-namespace.servicebus.windows.net/",
            false)]
        [DataRow(
            "sb://active-sb-namespace.servicebus.windows.net/",
            "sb://active-sb-namespace.servicebus.windows.net/",
            true,
            DisplayName = "When connected through MSI there isn't ';' in connection string.")]
        public void AreSameNamespaces_GivenTwoNamespaces_ShouldDetermineIfSame(
            string first, string second, bool areSame)
        {
            bool actual = MessagingHelper.AreSameNamespaces(first, second);
            Assert.AreEqual(areSame, actual);
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
