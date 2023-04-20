namespace Exos.Platform.AspNetCore.IntegrationTests.Policies
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Extensions.Http;

    public class TestHttpRequestResiliencyPolicy : IHttpRequestResiliencyPolicy
    {
        private static Random _jitterer = new Random();
        private HttpRequestPolicyOptions _options;
        private ILogger<HttpRequestResiliencyPolicy> _logger;
        private TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpRequestResiliencyPolicy"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException"> Thrown when one or more required arguments are null.</exception>
        /// <param name="options"> Options to configure HttpRequestResiliencyPolicy.</param>
        /// <param name="logger"> The logger.</param>
        /// <param name="telemetryClient"> The telemetry client.</param>
        public TestHttpRequestResiliencyPolicy(
            IOptions<HttpRequestPolicyOptions> options,
            ILogger<HttpRequestResiliencyPolicy> logger,
            TelemetryClient telemetryClient)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryClient = telemetryClient;

            // Handles HttpRequestException, Http status codes >= 500 (server errors) and status code 408 (request timeout)
            // plus extra http status codes
            ResilientPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => _options.RetryStatusCode.Contains((int)response.StatusCode))
                .WaitAndRetryAsync(
                    retryCount: _options.RetryAttempts,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100)),
                    onRetry: (result, sleepDuration, retryAttempt, context) =>
                    {
                        Exception retryException = result.Exception ?? new HttpRequestException($"StatusCode: `{result.Result.StatusCode}`.");

                        // if application doesn't have TelemetryClient, then skip populating exception telemetry.
                        if (_telemetryClient != null)
                        {
                            var telemetry = new ExceptionTelemetry(retryException);
                            telemetry.SeverityLevel = SeverityLevel.Critical;
                            telemetry.Message = $"Retry exception {retryException.Message}";
                            telemetry.Properties["SleepDuration"] = sleepDuration.ToString();
                            telemetry.Properties["OperationKey"] = context.OperationKey;
                            telemetry.Properties["Attempts"] = retryAttempt.ToString(CultureInfo.InvariantCulture);
                            _telemetryClient.TrackException(telemetry);
                        }

                        // this is only added here for testing verification
                        if (context != null && context.Contains("retryCount"))
                        {
                            context["retryCount"] = (int)context["retryCount"] + 1;
                        }

                        _logger.LogError(retryException, "HttpRequest Retry exception, {message}", retryException.Message);
                    });
        }

        /// <summary>
        /// Gets the HTTP request policy.
        /// </summary>
        public IsPolicy ResilientPolicy { get; }
    }
}
