#pragma warning disable SA1600 // Elements should be documented

namespace FeatureFlagSample
{
    public class WeatherForecast
    {
        public bool AppScopeFeatureFlag { get; set; }

        public bool GlobalScopeFeatureFlag { get; set; }

        public bool OtherScopeFeatureFlag { get; set; }

        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }
}