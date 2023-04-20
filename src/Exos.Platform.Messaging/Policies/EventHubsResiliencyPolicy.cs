namespace Exos.Platform.Messaging.Policies
{
    using System;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;

    /// <summary>
    /// This is used for Event hubs based operations, includes re-tries etc.
    /// </summary>
    public class EventHubsResiliencyPolicy : IEventHubsResiliencyPolicy
    {
        /// <summary>
        /// EventHubsResiliencyPolicyName.
        /// </summary>
        public const string EventHubsResiliencyPolicyName = "EventHubsResiliencyPolicy";
        private readonly ILogger<EventHubsResiliencyPolicy> _logger;
        private readonly EventHubsResiliencyPolicyOptions _options;
        private readonly TelemetryClient _telemetryClient;
        private IsPolicy _eventHubsPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsResiliencyPolicy"/> class.
        /// </summary>
        /// <param name="eventHubsPolicyOptionsAccessor">eventHubsPolicyOptionsAccessor.</param>
        /// <param name="logger">logger.</param>
        /// <param name="telemetryClient">telemetryClient.</param>
        public EventHubsResiliencyPolicy(IOptions<EventHubsResiliencyPolicyOptions> eventHubsPolicyOptionsAccessor, ILogger<EventHubsResiliencyPolicy> logger, TelemetryClient telemetryClient = null)
        {
            _logger = logger;
            if (eventHubsPolicyOptionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(eventHubsPolicyOptionsAccessor));
            }

            _options = eventHubsPolicyOptionsAccessor.Value;
            _telemetryClient = telemetryClient;
            _eventHubsPolicy = Policy
              .Handle<Exception>(ex =>
              {
                  if (ex is EventHubsException)
                  {
                      // Let it re-try for all Transient exceptions.
                      if (ex is EventHubsException actualEx && actualEx.IsTransient)
                      {
                          return true;
                      }
                      else
                      {
                          return false;
                      }
                  }
                  else
                  {
                      return true; // Handle socket expcetions, let it re-try.
                  }
              })
               .WaitAndRetryAsync(
                   retryCount: _options.MaxRetries,
                   sleepDurationProvider: attempt =>
                   {
                       var seconds = 1.0 * Math.Pow(2, attempt - 1); // Exponential back-off; 1s, 2s, 4s, 8s, 16s, 32s, etc...
                       return TimeSpan.FromSeconds(Math.Min(seconds, 30)); // Clamp wait to 50s max
                   },
                   onRetry: (exception, sleepDuration, context) =>
                   {
                       if (_telemetryClient != null)
                       {
                           var telemetry = new ExceptionTelemetry(exception);
                           telemetry.SeverityLevel = SeverityLevel.Critical;
                           telemetry.Message = $"Events Hub Retry exception {exception?.Message}";
                           telemetry.Properties["SleepDuration"] = sleepDuration.ToString();
                           telemetry.Properties["OperationKey"] = context.OperationKey;

                           _telemetryClient.TrackException(telemetry);
                       }

                       if (_logger != null)
                       {
                           _logger.LogError(exception, "EventHubs Retry exception, {message}", exception?.Message);
                       }
                   });
        }

        /// <summary>
        /// Gets EventHubsPolicy.
        /// </summary>
        public IsPolicy ResilientPolicy
        {
            get { return _eventHubsPolicy; }
        }
    }
}
