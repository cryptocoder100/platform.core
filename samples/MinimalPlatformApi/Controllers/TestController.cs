using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Exos.MinimalPlatformApi.Controllers
{
    /// <summary>
    /// TestController.
    /// </summary>
    [AllowAnonymous]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<BasicAuthTestController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestController"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{BasicAuthTestController}"/>.</param>
        /// <param name="httpClientFactory">The httpClientFactory<see cref="IHttpClientFactory"/>.</param>
        public TestController(ILogger<BasicAuthTestController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Throws an exception.
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpGet]
        public Task<IActionResult> ThrowException()
        {
            throw new ArgumentOutOfRangeException("id", "The ID was out of range.");
        }

        /// <summary>
        /// Test telemetry masking.
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpGet("/telemetrymasking")]
        public async Task<IActionResult> TestTelemetry()
        {
            var sb = new StringBuilder();
            sb.Append("https://hokuapi-uat.collateralanalytics.com/ResourceProxy?auth=");
            sb.Append("J5ueYSZgpWMyDW4ohWv9bmM1XBZyscWCQb529KF7QRppWS0lIeXjVXbOBAptgjC4M5GQmbb5y8SpBLjWgLCw1u44x%2");
            sb.Append("fEWDojdecuS6f0Ax9G4%2bY6%2bDVLQr2S6UqaF7APXeN%2bcNwpjJFUyGXgn0WjiUgeKgqQk5m9EbbfQVd%2fMwLs%3d");
            var uri = new Uri(sb.ToString());
            await SendHttpRequestAsync(uri);
            uri = new Uri("https://avssvc.uat2.exostechnology.internal/api/v1/requests/inbound/?Username=avsusername&Password=p@ssw0rd");
            await SendHttpRequestAsync(uri);
            return Ok("Sent HTTP Requests");
        }

        private async Task SendHttpRequestAsync(Uri uri)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Client_No_Header");
                using var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseModel = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogInformation($"HTTP response: {responseModel}");
            }
            catch (Exception ex)
            {
                var httpRequestException = new HttpRequestException($"An error occurred while sending the request. Uri: '{uri}', Method: 'GET'.", ex);
                throw httpRequestException;
            }
        }
    }
}
