#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1801 // Review unused parameters

namespace Exos.Platform.AspNetCore.UnitTests.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IWebHostBuildExtensionsTest
    {
        [TestMethod]
        public void ExternalJsonFiles()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IWebHostBuilderExtensionsTest");
            Environment.SetEnvironmentVariable("TestSetting4", "Env Value 4");

            var webHost = WebHost.CreateDefaultBuilder()
                .UsePlatformExternalJsonFilesConfiguration()
                .UseStartup<IWebHostBuildExtensionsTest>()
                .Build();
            Assert.IsNotNull(webHost);

            var config = webHost.Services.GetService<IConfiguration>();
            Assert.IsNotNull(config);
            Assert.AreEqual("Base Value 1", config.GetValue<string>("TestSetting1"));
            Assert.AreEqual("File1 Value 2", config.GetValue<string>("TestSetting2"));
            Assert.AreEqual("File2 Value 3", config.GetValue<string>("TestSetting3"));
            Assert.AreEqual("Env Value 4", config.GetValue<string>("TestSetting4"));
        }

        [TestMethod]
        public void CustomExternalJsonFiles()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IWebHostBuilderExtensionsTest");
            Environment.SetEnvironmentVariable("TestSetting4", "Env Value 4");

            var webHost = WebHost.CreateDefaultBuilder()
                .UsePlatformExternalJsonFilesConfiguration("CustomExternalJsonFiles")
                .UseStartup<IWebHostBuildExtensionsTest>()
                .Build();
            Assert.IsNotNull(webHost);

            var config = webHost.Services.GetService<IConfiguration>();
            Assert.IsNotNull(config);
            Assert.AreEqual("Base Value 1", config.GetValue<string>("TestSetting1"));
            Assert.AreEqual("File1 Value 2", config.GetValue<string>("TestSetting2"));
            Assert.AreEqual("File1 Value 3", config.GetValue<string>("TestSetting3"));
            Assert.AreEqual("Env Value 4", config.GetValue<string>("TestSetting4"));
        }

        [TestMethod]
        [Ignore]
        public void AzureKeyVault_Initialization_Test()
        {
            /*
             * Make sure that we have environment variable set inorder to run the test.
             * Commenting out the test t
             */

            var webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<IWebHostBuildExtensionsTest>()
                // .UsePlatformExternalJsonFilesConfiguration()
                .UsePlatformConfigurationDefaults()
                .ConfigureServices(ConfigureServices)
                .Build();
            Assert.IsNotNull(webHost);
            var config = webHost.Services.GetService<IConfiguration>();

            var encryptionUtil = webHost.Services.GetService<ISecureEncryption>();
            Assert.IsNotNull(config);
            Assert.IsNotNull(encryptionUtil);
            Assert.AreEqual("None", config.GetValue<string>("ExosKeyVault:Certificate"));
            string keyName = "SSNKey";
            string plainText = "Test Encryption string";
            string encryptedData = encryptionUtil.EncryptPiiData(plainText, keyName);
            string decriptedData = encryptionUtil.DecriptPiiData(encryptedData);
            Assert.AreEqual(plainText, decriptedData);
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ISecureEncryption, AesEncryption>();
        }
    }

    public class UserContextOptions
    {
        public UserContextOptions()
        {
        }

        public bool UseInternalToken { get; set; }

        public int ContextCacheDuration { get; set; }

        public string IgnoreInputPattern { get; set; }

        public string UserSvc { get; set; }

        public string VendorManagementSvc { get; set; }

        public string ClientManagementSvc { get; set; }

        public string OrderManagementSvc { get; set; }

        public string JwtSigningKey { get; set; }

        public string JwtIssuer { get; set; }

        public string JwtAudience { get; set; }

        public int JwtLifetimeInMinutes { get; set; }
    }
}
#pragma warning restore SA1204 // Static elements should appear before instance elements
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1801 // Review unused parameters