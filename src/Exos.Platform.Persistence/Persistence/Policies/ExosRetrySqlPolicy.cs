namespace Exos.Platform.Persistence.Policies
{
    using System;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;

    /// <summary>
    /// This is used for Sql connection based operations, includes re-tries etc.
    /// </summary>
    public class ExosRetrySqlPolicy : IExosRetrySqlPolicy
    {
        private readonly ILogger<ExosRetrySqlPolicy> _logger;
        private readonly SqlRetryPolicyOptions _sqlRetryPolicyOptions;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosRetrySqlPolicy"/> class.
        /// </summary>
        /// <param name="sqlRetryPolicyOptions"><see cref="SqlRetryPolicyOptions"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        public ExosRetrySqlPolicy(IOptions<SqlRetryPolicyOptions> sqlRetryPolicyOptions, ILogger<ExosRetrySqlPolicy> logger, TelemetryClient telemetryClient = null)
        {
            if (sqlRetryPolicyOptions == null)
            {
                throw new ArgumentNullException(nameof(sqlRetryPolicyOptions));
            }

            _sqlRetryPolicyOptions = sqlRetryPolicyOptions != null ? sqlRetryPolicyOptions.Value : throw new ArgumentNullException(nameof(sqlRetryPolicyOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryClient = telemetryClient;

            ResilientPolicy = Policy
               .Handle<SqlException>(ex => SqlServerTransientExceptionDetector.ShouldRetryOn(ex))
               .WaitAndRetryAsync(
                   retryCount: _sqlRetryPolicyOptions.MaxRetries,
                   sleepDurationProvider: attempt =>
                   {
                       var seconds = 1.0 * Math.Pow(2, attempt - 1); // Exponential back-off; 1s, 2s, 4s, 8s, 16s, 32s, etc...
                       return TimeSpan.FromSeconds(Math.Min(seconds, _sqlRetryPolicyOptions.CommandTimeout)); // Clamp wait to 30s max
                   },
                   onRetry: (exception, sleepDuration, context) =>
                   {
                       // Report the connection as failed; we get a new one on the next loop
                       if (_telemetryClient != null)
                       {
                           var telemetry = new ExceptionTelemetry(exception)
                           {
                               SeverityLevel = SeverityLevel.Critical,
                               Message = $"Retry exception {exception?.Message}"
                           };
                           telemetry.Properties["SleepDuration"] = sleepDuration.ToString();
                           telemetry.Properties["OperationKey"] = context.OperationKey;

                           _telemetryClient.TrackException(telemetry);
                       }

                       _logger.LogError(exception, $"Sql Retry exception, {exception?.Message} ");
                   });
        }

        /// <inheritdoc/>
        public IsPolicy ResilientPolicy { get; }
    }
}