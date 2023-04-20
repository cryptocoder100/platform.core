namespace Exos.Platform.AspNetCore.UnitTests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.AppConfiguration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExosAzureConfigurationResolutionProcessorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullBuilder()
        {
            // Act
            var resolver = new ExosAzureConfigurationResolver(null);
        }

        [TestMethod]
        public void SingleMacroResolve()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { "ValueWithToken", "${TokenTest}" },
                { "ExosMacro:TokenTest", "TokenValue" }
            };
            var configuration = GetConfiguration(configValues);
            var resolver = new ExosAzureConfigurationResolver(configuration);

            // Act
            var errorBuilder = resolver.ProcessTokens();

            // Assert
            Assert.AreEqual("TokenValue", configuration["ValueWithToken"]);
            Assert.IsTrue(errorBuilder.Length == 0);
        }

        [TestMethod]
        public void ReferencingUnregisteredMacro()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { "ValueWithToken", "${TokenTest}" },
            };
            var configuration = GetConfiguration(configValues);
            var resolver = new ExosAzureConfigurationResolver(configuration);

            // Act
            var errorBuilder = resolver.ProcessTokens();

            // Assert
            Assert.IsTrue(errorBuilder.Length > 0);
        }

        [TestMethod]
        public void NestedMacroResolve()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                { "ValueWithToken", "Alpha_${First}_Beta_${Second}" },
                { "ExosMacro:First", "1" },
                { "ExosMacro:Second", "2" },
            };

            var configuration = GetConfiguration(configValues);
            var resolver = new ExosAzureConfigurationResolver(configuration);

            // Act
            var errorBuilder = resolver.ProcessTokens();

            // Assert
            Assert.AreEqual("Alpha_1_Beta_2", configuration["ValueWithToken"]);
            Assert.IsTrue(errorBuilder.Length == 0);
        }

        private static IConfiguration GetConfiguration(IReadOnlyDictionary<string, string> configurations)
        {
            var providers = new List<IConfigurationProvider> { new CustomConfigurationProvider(configurations) };
            var root = new ConfigurationRoot(providers);
            return root;
        }

        private class CustomConfigurationProvider : ConfigurationProvider
        {
            private readonly IReadOnlyDictionary<string, string> _configurations;

            public CustomConfigurationProvider(IReadOnlyDictionary<string, string> configurations)
            {
                _configurations = configurations;
            }

            public override void Load()
            {
                foreach (var (key, value) in _configurations)
                {
                    Set(key, value);
                }
            }
        }
    }
}
