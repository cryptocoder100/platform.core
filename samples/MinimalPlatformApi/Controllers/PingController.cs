namespace Exos.MinimalPlatformApi.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// PingController.
    /// </summary>
    [AllowAnonymous]
    [Route("api/v1/[controller]")]
    public class PingController : ControllerBase
    {
        private readonly IConfigurationRoot _configurationRoot;
        private readonly ILogger<PingController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PingController"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger" /> instance.</param>
        /// <param name="configuration">An <see cref="IConfiguration" /> instance.</param>
        public PingController(ILogger<PingController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configurationRoot = (IConfigurationRoot)configuration;
        }

        /// <summary>
        /// Respond to let the monitoring system know this instance is still responding.
        /// </summary>
        /// <returns>An asynchronous result that yields the asynchronous.</returns>
        [HttpGet]
        public Task<IActionResult> GetAsync()
        {
            _logger.LogInformation($"Test trace from {nameof(PingController)}.");
            return Task.FromResult((IActionResult)Ok());
        }

        /// <summary>
        /// Forces the service to reload its configuration.
        /// </summary>
        [HttpGet("reload")]
        public Task<IActionResult> ReloadConfig()
        {
            _configurationRoot.Reload();
            return Task.FromResult((IActionResult)Ok());
        }
    }
}
