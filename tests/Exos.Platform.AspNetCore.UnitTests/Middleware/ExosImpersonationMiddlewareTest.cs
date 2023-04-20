namespace Exos.Platform.AspNetCore.UnitTests.Middleware
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Security.Authentication;
    using System.Security.Claims;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ExosImpersonationMiddlewareTest
    {
        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void ProcessAdditionalContext_NoHash()
        {
            // Arrange
            const string additionalContext = "username|email|expirationticks";

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(additionalContext, null);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void ProcessAdditionalContext_InvalidHash()
        {
            // Arrange
            const string additionalContext = "username|email|expirationticks~1.23";

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(additionalContext, null);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void ProcessAdditionalContext_MissingUsername()
        {
            // Arrange
            const string additionalContext = "|email|expirationticks~1.23";

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(additionalContext, null);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void ProcessAdditionalContext_MissingEmail()
        {
            // Arrange
            const string additionalContext = "username||expirationticks~1.23";

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(additionalContext, null);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void ProcessAdditionalContext_ValidNoContext()
        {
            // Arrange
            var principal = new ClaimsPrincipal();

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(null, principal);

            // Assert
            Assert.IsNull(principal.FindFirst(ClaimTypes.NameIdentifier));
            Assert.IsNull(principal.FindFirst(ClaimTypes.Email));
        }

        [TestMethod]
        public void ProcessAdditionalContext_Valid()
        {
            // Arrange
            var principal = new ClaimsPrincipal();
            var claimIdentity = new ClaimsIdentity();
            principal.AddIdentity(claimIdentity);
            var expectedDetail = $"username|email|{GetJwtExpiration()}";
            var hash = ApiKeySecurity.Version1.HashPassword(expectedDetail);
            expectedDetail += $"~{hash}";

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(expectedDetail, principal);

            // Assert
            Assert.IsTrue(claimIdentity.HasClaim(ClaimTypes.NameIdentifier, "username"));
            Assert.IsTrue(claimIdentity.HasClaim(ClaimTypes.Email, "email"));
        }

        [TestMethod]
        public void ProcessAdditionalContext_ValidFromTokenProvider()
        {
            // Arrange
            var principal = new ClaimsPrincipal();
            var claimIdentity = new ClaimsIdentity();
            principal.AddIdentity(claimIdentity);
            var provider = new B2CAppTokenProvider(null);
            var actualContext = provider.GetAdditionalUserContext("username", "email", GetJwtExpiration());

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(actualContext, principal);

            // Assert
            Assert.IsTrue(claimIdentity.HasClaim(ClaimTypes.NameIdentifier, "username"));
            Assert.IsTrue(claimIdentity.HasClaim(ClaimTypes.Email, "email"));
        }

        [TestMethod]
        public void ProcessAdditionalContext_ValidReplacingExistingClaims()
        {
            // Arrange
            var principal = new ClaimsPrincipal();
            var claimIdentity = new ClaimsIdentity();
            claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "PreviousUserName"));
            claimIdentity.AddClaim(new Claim(ClaimTypes.Email, "PreviousEmail"));
            principal.AddIdentity(claimIdentity);
            var provider = new B2CAppTokenProvider(null);
            var actualContext = provider.GetAdditionalUserContext("username", "email", GetJwtExpiration());

            // Act
            ExosImpersonationMiddleware.ProcessAdditionalContext(actualContext, principal);

            // Assert
            Assert.IsTrue(claimIdentity.HasClaim(ClaimTypes.NameIdentifier, "username"));
            Assert.IsTrue(claimIdentity.HasClaim(ClaimTypes.Email, "email"));
        }

        private static string GetJwtExpiration()
        {
            return DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString(CultureInfo.CurrentCulture);
        }
    }
}
