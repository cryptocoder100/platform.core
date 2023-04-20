namespace Exos.MinimalPlatformApi.Controllers
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Controller to test the features that the auction services need.
    /// </summary>
    [Route("api/v1/[controller]")]
    public class BasicAuthTestController : ControllerBase
    {
        private readonly ILogger<BasicAuthTestController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiKeyAuthenticationOptions _apiKeyOptions;
        private readonly HttpClient _userClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthTestController"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{BasicAuthTestController}"/>.</param>
        /// <param name="httpClientFactory">The httpClientFactory<see cref="IHttpClientFactory"/>.</param>
        /// <param name="apiKeyOptions">The apiKeyOptions<see cref="IOptions{ApiKeyAuthenticationOptions}"/>.</param>
        public BasicAuthTestController(ILogger<BasicAuthTestController> logger, IHttpClientFactory httpClientFactory, IOptions<ApiKeyAuthenticationOptions> apiKeyOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _apiKeyOptions = apiKeyOptions?.Value ?? throw new ArgumentNullException(nameof(apiKeyOptions));
            _userClient = _httpClientFactory.CreateClient("Client_Authentication_Header_and_Tracking_Id");
        }

        /// <summary>
        /// Performs standard Get processing.
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var uri = new Uri(_apiKeyOptions.UserSvc + "/api/v1/users/current");

                try
                {
                    using (var response = await _userClient.GetAsync(uri).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var userModel = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return Ok(userModel);
                    }
                }
                catch (Exception ex)
                {
                    // Log and add some more data
                    var ex1 = new HttpRequestException($"An error occurred while sending the request. Uri: '{uri}', Method: 'GET'.", ex);
                    _logger.LogWarning(ex, "There was an error calling the UserSvc for username lookup. Uri: '{uri}', Method: '{method}'", uri, "GET");
                    throw ex1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Query error: {ex.Message}");
                throw;
            }
        }
    }
}
