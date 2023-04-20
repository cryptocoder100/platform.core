namespace Exos.MinimalPlatformService.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Exos.MinimalPlatformService.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service Status Controller.
    /// </summary>
    [Route("api/v1/[controller]")]
    public class StatusController : Controller
    {
        private readonly StatusModel _status;
        private readonly ILogger<StatusController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusController"/> class.
        /// </summary>
        /// <param name="status">StatusModel.</param>
        /// <param name="logger">Logger instance.</param>
        public StatusController(StatusModel status, ILogger<StatusController> logger)
        {
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Evaluation Level.
        /// </summary>
        public enum EvaluationLevel
        {
            /// <summary>
            /// Monitoring Evaluation Level.
            /// </summary>
            Monitoring,

            /// <summary>
            /// Deployment Evaluation Level.
            /// </summary>
            Deployment,

            /// <summary>
            /// Diagnostic Evaluation Level.
            /// </summary>
            Diagnostic,
        }

        /// <summary>
        /// Get the current status of the service.
        /// </summary>
        /// <param name="level">EvaluationLevel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(StatusModel), 200)]
        [AllowAnonymous]
        public Task<IActionResult> Get([FromQuery] EvaluationLevel level = EvaluationLevel.Monitoring)
        {
            _logger.LogDebug($"Level:{level.ToString()}");
            return Task.FromResult((IActionResult)Ok(_status));
        }
    }
}
