namespace Exos.MinimalPlatformApi.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using Exos.MinimalPlatformApi.Models;
    using Exos.Platform.AspNetCore.Models;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Controller to test the features that the auction services need.
    /// </summary>
    [Route("api/v1/[controller]")]
    public class AuctionTestController : ControllerBase
    {
        private readonly ILogger<AuctionTestController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuctionTestController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public AuctionTestController(ILogger<AuctionTestController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs standard Get processing.
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var identity = (ClaimsIdentity)HttpContext.User.Identity;

                var claims = identity.Claims.Select(c => new ClaimModel(c.Type, c.ToString())).ToList();
                var email = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                var userClaims = new AuctionTestUserModel
                {
                    Claims = new UserClaims { Claims = claims },
                    UserId = identity.GetUserId(),
                    Email = email
                };

                _logger.LogInformation($"Claims: {userClaims}");
                return Ok(userClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Query error: {ex.Message}");
                throw;
            }
        }
    }
}
