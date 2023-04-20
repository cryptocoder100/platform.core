namespace Exos.Platform.Messaging.Policies
{
    using System;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;

    /// <summary>
    /// This is used for Event hubs based operations, includes re-tries etc.
    /// </summary>
    public class ServiceBusResiliencyPolicy : IServiceBusResiliencyPolicy
    {
        /// <summary>
        /// ServiceBusResiliencyPolicyName.
        /// </summary>
        public const string ServiceBusResiliencyPolicyName = "ServiceBusResiliencyPolicy";

        private readonly ILogger<ServiceBusResiliencyPolicy> _logger;
        private readonly ServiceBusResiliencyPolicyOptions _options;
        private readonly TelemetryClient _telemetryClient;
        private IsPolicy _serviceBusPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusResiliencyPolicy"/> class.
        /// </summary>
        /// <param name="serviceBusPolicyOptionsAccessor">serviceBusPolicyOptionsAccessor.</param>
        /// <param name="logger">logger.</param>
        /// <param name="telemetryClient">telemetryClient.</param>
        public ServiceBusResiliencyPolicy(IOptions<ServiceBusResiliencyPolicyOptions> serviceBusPolicyOptionsAccessor, ILogger<ServiceBusResiliencyPolicy> logger, TelemetryClient telemetryClient = null)
        {
            _logger = logger;
            if (serviceBusPolicyOptionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(serviceBusPolicyOptionsAccessor));
            }

            _options = serviceBusPolicyOptionsAccessor.Value;
            _telemetryClient = telemetryClient;
            _serviceBusPolicy = Policy
               .Handle<Exception>(ex =>
               {
                   if (ex is ServiceBusException)
                   {
                       var actualEx = ex as ServiceBusException;

                       // Let it re-try for all Transient exceptions.
                       if (actualEx.IsTransient)
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
                       return TimeSpan.FromSeconds(Math.Min(seconds, 30)); // Clamp wait to 30s max
                   },
                   onRetry: (exception, sleepDuration, context) =>
                   {
                       if (_telemetryClient != null)
                       {
                           var telemetry = new ExceptionTelemetry(exception)
                           {
                               SeverityLevel = SeverityLevel.Critical,
                               Message = $"ServiceBus Retry exception {exception?.Message}"
                           };
                           telemetry.Properties["SleepDuration"] = sleepDuration.ToString();
                           telemetry.Properties["OperationKey"] = context.OperationKey;

                           _telemetryClient.TrackException(telemetry);
                       }

                       if (_logger != null)
                       {
                           _logger.LogError(exception, "ServiceBus Retry exception, {message}", exception?.Message);
                       }
                   });
        }

        /// <summary>
        /// Gets ServiceBusPolicy.
        /// </summary>
        public IsPolicy ResilientPolicy
        {
            get { return _serviceBusPolicy; }
        }
    }
}
