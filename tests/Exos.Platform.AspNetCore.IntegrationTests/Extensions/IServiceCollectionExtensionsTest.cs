#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable CA2000 // Dispose objects before losing scope
namespace Exos.Platform.AspNetCore.IntegrationTests.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.IntegrationTests.Options;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Polly;
    using Polly.Registry;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class IServiceCollectionExtensionsTest
    {
        private static TestServer _testServer;
        private IHttpClientFactory _httpClientFactory;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.IServiceCollectionExtensionsTest.ResiliencyPolicy.json");

            IConfiguration configuration = builder.Build();

            _testServer = new TestServer(new WebHostBuilder()
                .UsePlatformConfigurationDefaults()
                .UseConfiguration(configuration)
                .UseStartup<TestStartup>());
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
        public void CreateHttpClientGivenNameShouldCreateHttpClient()
        {
            var contextClient = _httpClientFactory.CreateClient(HttpClientTypeConstants.Context);
            var naiveClient = _httpClientFactory.CreateClient(HttpClientTypeConstants.Naive);

            Assert.IsNotNull(contextClient);
            Assert.IsNotNull(naiveClient);
        }

        [TestMethod]
        [DataRow(HttpClientTypeConstants.Naive)]
        [DataRow(HttpClientTypeConstants.Context)]
        public async Task SendHttpRequestGivenInvalidUriShouldFail(string type)
        {
            var client = _httpClientFactory.CreateClient(type);
            var resp = await client.SendAsync(new HttpRequestMessage
            {
                RequestUri = new Uri("https://httpstat.us/503"),
                Method = HttpMethod.Get,
            }).ConfigureAwait(false);

            Assert.IsNotNull(resp);
            Assert.AreEqual(503, (int)resp.StatusCode);
        }

        [TestMethod]
        public async Task ClientWithoutPolicyGivenPolicyShouldRetry()
        {
            var option = _testServer.Host.Services.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
            var client = _httpClientFactory.CreateClient("NoPolicyAtDI");
            var policy = _testServer.Host.Services.GetService<IReadOnlyPolicyRegistry<string>>()
                    .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http);
            var context = new Polly.Context { { "retryCount", 0 } };

            var resp = await policy.ExecuteAsync(
                ct => client.SendAsync(
                    new HttpRequestMessage
                    {
                        RequestUri = new Uri("https://httpstat.us/503"),
                        Method = HttpMethod.Get,
                    }),
                context);

            Assert.AreEqual(option.RetryAttempts, (int)context["retryCount"]);
        }

        [TestMethod]
        public async Task SendHttpRequestGivenPolicyNameShouldRetry()
        {
            var option = _testServer.Host.Services.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
            var client = _httpClientFactory.CreateClient(HttpClientTypeConstants.Context);
            var policy = _testServer.Host.Services.GetService<IReadOnlyPolicyRegistry<string>>()
                    .Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http);
            var context = new Polly.Context { { "retryCount", 0 } };

            var resp = await policy.ExecuteAsync(
                ct => client.SendAsync(
                    new HttpRequestMessage
                    {
                        RequestUri = new Uri("https://httpstat.us/503"),
                        Method = HttpMethod.Get,
                    }),
                context);

            Assert.AreEqual(option.RetryAttempts, (int)context["retryCount"]);
        }

        [TestMethod]
        public async Task ContextHttpClientGivenCorrectEndpointShouldSucceed()
        {
            var option = _testServer.Host.Services.GetService<IOptions<ExternalServiceOptions>>().Value;
            var userClient = _httpClientFactory.CreateClient("UserSvc");
            HttpResponseMessage resp;
            using (var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    string.Format(CultureInfo.InvariantCulture, option.ExternalServices["UserSvc"].Args["Contexts"], "developertest@testsvclnk.com")))
            {
                resp = await userClient.SendAsync(request).ConfigureAwait(false);
            }

            Assert.AreEqual(200, (int)resp.StatusCode);
        }

        [TestMethod]
        public async Task NaiveHttpClientGivenCorrectEndpointShouldFail()
        {
            var option = _testServer.Host.Services.GetService<IOptions<ExternalServiceOptions>>().Value;
            var naiveClient = _httpClientFactory.CreateClient(HttpClientTypeConstants.Naive);
            HttpResponseMessage resp;
            var host = option.ExternalServices["UserSvc"].Host;
            using (var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{host}/{string.Format(CultureInfo.InvariantCulture, option.ExternalServices["UserSvc"].Args["Contexts"], "developertest@testsvclnk.com")}")
            })
            {
                resp = await naiveClient.SendAsync(request).ConfigureAwait(false);
            }

            Assert.AreEqual(200, (int)resp.StatusCode);
        }

        [TestMethod]
        public async Task RetryRequestsGivenTargetStatusCodeShouldHaveLongerResponseTime()
        {
            Stopwatch w1 = new Stopwatch();
            w1.Start();
            await ContextHttpClientGivenCorrectEndpointShouldSucceed().ConfigureAwait(false);
            w1.Stop();

            Stopwatch w2 = new Stopwatch();
            w2.Start();
            await SendHttpRequestGivenPolicyNameShouldRetry().ConfigureAwait(false);
            w2.Stop();

            long t1 = w1.ElapsedMilliseconds, t2 = w2.ElapsedMilliseconds;
            Assert.IsTrue(t2 > t1);
        }

        [TestMethod]
        public async Task RetryShouldOnlyBeAppliedToSpecifiedHttpMethod()
        {
            var option = _testServer.Host.Services.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
            var client = _httpClientFactory.CreateClient("RetryOnGetAndPut");
            var context = new Polly.Context { { "retryCount", 0 } };

            HttpResponseMessage resp;
            using (var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://httpstat.us/503"),
                Method = HttpMethod.Get,
            })
            {
                request.SetPolicyExecutionContext(context);
                resp = await client.SendAsync(request).ConfigureAwait(false);
            }

            Assert.AreEqual(option.RetryAttempts, (int)context["retryCount"]);

            // context = new Polly.Context { { "retryCount", 0 } };
            using (var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://httpstat.us/504"),
                Method = HttpMethod.Put,
            })
            {
                request.SetPolicyExecutionContext(context);
                resp = await client.SendAsync(request).ConfigureAwait(false);
            }

            Assert.AreEqual(option.RetryAttempts, (int)context["retryCount"]);
        }

        [TestMethod]
        public async Task RetryShouldNotBeAppliedIfHttpMethodDoesNotMatch()
        {
            var option = _testServer.Host.Services.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
            var client = _httpClientFactory.CreateClient("RetryOnGetAndPut");
            var context = new Polly.Context { { "retryCount", 0 } };

            HttpResponseMessage resp;
            using (var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://httpstat.us/503"),
                Method = HttpMethod.Post,
            })
            {
                request.SetPolicyExecutionContext(context);
                resp = await client.SendAsync(request).ConfigureAwait(false);
            }

            Assert.AreEqual(0, (int)context["retryCount"]);
        }
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore CA2000 // Dispose objects before losing scope