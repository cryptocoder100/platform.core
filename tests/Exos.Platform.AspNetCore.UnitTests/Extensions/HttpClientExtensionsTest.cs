#pragma warning disable CA1001 // Types that own disposable fields should be disposable
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Exos.Platform.AspNetCore.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Exos.Platform.AspNetCore.UnitTests.Extensions
{
    [TestClass]
    public class HttpClientExtensionsTest
    {
        private readonly Dictionary<string, string> _headers = new ()
        {
            { "Accept", "application/json" },
            { "tracking-id", Guid.NewGuid().ToString() }
        };

        private readonly StringContent _stringContent = new ("[{'id':1,'value':'1'}]");

        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;

        [TestInitialize]
        public void Initialize()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        }

        [TestMethod]
        public void SendAsync_With_Content_and_Header()
        {
            // Arrange
            string targetUri = $"http://dummy.restapiexample.com/api/v1/employee/1";
            // Setup sendAsync method for HttpMessage Handler Mock
            using HttpResponseMessage httpResponseMessage = new ()
            {
                StatusCode = HttpStatusCode.OK,
                Content = _stringContent,
            };
            _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(httpResponseMessage)
                .Verifiable();

            // Act
            var response = _httpClient.SendAsync(HttpMethod.Get, new Uri(targetUri), _headers, _stringContent);

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1), // verify number of times SendAsync is called
                ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get // verify the HttpMethod for request is GET
                && req.RequestUri.ToString() == targetUri // verify the RequestUri is as expected
                && req.Headers.GetValues("Accept").FirstOrDefault() == "application/json" // Verify Accept header
                && req.Headers.GetValues("tracking-id").FirstOrDefault() != null), // Verify tracking-id header is added
                ItExpr.IsAny<CancellationToken>());
            Assert.IsTrue(response?.Result.IsSuccessStatusCode);
        }

        [TestMethod]
        public void SendAsync_With_Bearer_Token()
        {
            // Arrange
            string targetUri = $"http://dummy.restapiexample.com/api/v1/employee/1";
            // Setup sendAsync method for HttpMessage Handler Mock
            using HttpResponseMessage value = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = _stringContent,
            };
            _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(value)
                .Verifiable();

            // Act
            var response = _httpClient?.SendRequestWithBearerTokenAsync(HttpMethod.Get, new Uri(targetUri), Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("Exos:Platform")), _headers);

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1), // verify number of times SendAsync is called
                ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get // verify the HttpMethod for request is GET
                && req.RequestUri.ToString() == targetUri // verify the RequestUri is as expected
                && req.Headers.GetValues("Accept").FirstOrDefault() == "application/json" // Verify Accept header
                && req.Headers.GetValues("tracking-id").FirstOrDefault() != null),
                ItExpr.IsAny<CancellationToken>());
            Assert.IsTrue(response?.Result.IsSuccessStatusCode);
        }
    }
}
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
