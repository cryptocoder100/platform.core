namespace Exos.Platform.AspNetCore.IntegrationTests.Security
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class HttpContextClientFactoryTests
    {
        private static TestServer _testServer;
        private IHttpClientFactory _httpClientFactory;

        [ClassInitialize]
#pragma warning disable CA1801 // Review unused parameters
        public static void ClassInit(TestContext context)
#pragma warning restore CA1801 // Review unused parameters
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.IServiceCollectionExtensionsTest.ResiliencyPolicy.json");

            IConfiguration configuration = builder.Build();

            _testServer = new TestServer(new WebHostBuilder()
                .UsePlatformConfigurationDefaults()
                .UseConfiguration(configuration)
                .UseStartup<HttpContextClientFactoryStartup>());
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
            _httpClientFactory = _testServer.Host.Services.GetService<IHttpClientFactory>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task HttpClientShouldSendRequest()
        {
            using var client = _httpClientFactory.CreateClient();
            using var requet = new HttpRequestMessage
            {
                RequestUri = new Uri("https://httpstat.us/200"),
                Method = HttpMethod.Get,
            };
            var resp = await client.SendAsync(requet).ConfigureAwait(false);

            Assert.IsNotNull(resp);
            Assert.AreEqual(200, (int)resp.StatusCode);
        }

        [TestMethod]
        public async Task PlatformHttpClientShouldInjectAuthAndTrackingIdToRequest()
        {
            var client = _testServer.Host.Services.GetService<HttpClient>();
            using var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://httpstat.us/200"),
                Method = HttpMethod.Get,
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "YOUR JWT TOKEN");
            var resp = await client.SendAsync(request).ConfigureAwait(false);
            request.Headers.TryGetValues("Authorization", out IEnumerable<string> authorizationHeader);

            Assert.IsNotNull(resp);
            Assert.AreEqual(200, (int)resp.StatusCode);
            Assert.IsTrue(authorizationHeader.First().StartsWith("Bearer ", StringComparison.InvariantCulture));
        }
    }
}
