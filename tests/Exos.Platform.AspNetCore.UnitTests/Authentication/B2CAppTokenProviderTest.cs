namespace Exos.Platform.AspNetCore.UnitTests.Authentication
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class B2CAppTokenProviderTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAdditionalUserContext_NullUsername()
        {
            // Arrange
            var provider = new B2CAppTokenProvider(null);

            // Act
            provider.GetAdditionalUserContext(null, "valid", "valid");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAdditionalUserContext_EmptyUsername()
        {
            // Arrange
            var provider = new B2CAppTokenProvider(null);

            // Act
            provider.GetAdditionalUserContext(string.Empty, "valid", "valid");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAdditionalUserContext_EmptyEmail()
        {
            // Arrange
            var provider = new B2CAppTokenProvider(null);

            // Act
            provider.GetAdditionalUserContext("valid", string.Empty, "valid");
        }

        [TestMethod]
        public void GetAdditionalUserContext_Valid()
        {
            // Arrange
            var provider = new B2CAppTokenProvider(null);
            const string expectedDetails = "username|email|expirationticks";

            // Act
            var actualContext = provider.GetAdditionalUserContext("username", "email", "expirationticks");

            // Assert
            Assert.IsNotNull(actualContext);
            var bits = actualContext.Split('~');
            Assert.AreEqual(2, bits.Length);
            Assert.IsTrue(ApiKeySecurity.Version1.ValidatePassword(expectedDetails, bits[1]));
        }
    }
}
