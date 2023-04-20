#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CA5394 // Do not use insecure randomness

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace FeatureFlagSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IFeatureManager _featureManager;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IFeatureManager featureManager)
        {
            _logger = logger;
            _featureManager = featureManager;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var appScopeFeatureFlag = await _featureManager.IsEnabledAsync("AppScopedFlag");
            var globalScopeFeatureFlag = await _featureManager.IsEnabledAsync("GlobalScopedFlag");
            var othersScopeFeatureFlag = await _featureManager.IsEnabledAsync("OtherScopedFlag");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                AppScopeFeatureFlag = appScopeFeatureFlag,
                GlobalScopeFeatureFlag = globalScopeFeatureFlag,
                OtherScopeFeatureFlag = othersScopeFeatureFlag,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}