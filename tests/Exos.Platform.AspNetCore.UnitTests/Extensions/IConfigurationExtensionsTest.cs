namespace Exos.Platform.AspNetCore.UnitTests.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IConfigurationExtensionsTest
    {
        [TestMethod]
        public void ReplaceTokensSuccess()
        {
            // Arrange
            var configuration = GetConfiguration();
            configuration["Token1"] = "Token1Value";
            configuration["RandomKey"] = "${Token1}";

            var errorBuilder = new StringBuilder();
            const string input = "Value-${Token1}";

            // Act
            var result = IConfigurationExtensions.ReplaceTokens(configuration, input, errorBuilder);

            // Assert
            const string resultString = "Value-Token1Value";
            Assert.AreEqual(resultString, result);
            Assert.AreEqual(0, errorBuilder.Length);
        }

        [TestMethod]
        public void ReplaceTokensInvalidKey()
        {
            // Arrange
            var configuration = GetConfiguration();
            configuration["Token1"] = "Token1Value";
            configuration["RandomKey"] = "${Token1}";

            var errorBuilder = new StringBuilder();

            const string input = "Value-${Token2}";

            // Act
            var result = IConfigurationExtensions.ReplaceTokens(configuration, input, errorBuilder);

            // Assert
            Assert.AreEqual(result, result);
            Assert.IsTrue(errorBuilder.Length > 0);
        }

        private static IConfiguration GetConfiguration()
        {
            var webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<IWebHostBuildExtensionsTest>()
                .Build();
            return webHost.Services.GetService<IConfiguration>();
        }
    }
}
