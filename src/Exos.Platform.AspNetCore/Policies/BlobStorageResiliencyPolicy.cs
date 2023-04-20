#pragma warning disable SA1402 // FileMayOnlyContainASingleType
namespace Exos.Platform.AspNetCore.Resiliency.Policies
{
    using System;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;

    /// <summary>
    /// This is used for blob connection based operations, includes re-tries etc.
    /// </summary>
    public class BlobStorageResiliencyPolicy : IBlobStorageResiliencyPolicy
    {
        private readonly ILogger<BlobStorageResiliencyPolicy> _logger;
        private readonly BlobStorageResiliencyPolicyOptions _options;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStorageResiliencyPolicy"/> class.
        /// </summary>
        /// <param name="blobStoragePolicyOptionsAccessor"><see cref="BlobStorageResiliencyPolicyOptions"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="telemetryClient"><see cref="TelemetryClient"/>.</param>
        public BlobStorageResiliencyPolicy(
            IOptions<BlobStorageResiliencyPolicyOptions> blobStoragePolicyOptionsAccessor,
            ILogger<BlobStorageResiliencyPolicy> logger,
            TelemetryClient telemetryClient = null)
        {
            _logger = logger;

            if (blobStoragePolicyOptionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(blobStoragePolicyOptionsAccessor));
            }

            _options = blobStoragePolicyOptionsAccessor.Value;
            _telemetryClient = telemetryClient;

            ResilientPolicy = Policy
               .Handle<StorageException>()
               .WaitAndRetryAsync(
                   retryCount: _options.MaxRetries,
                   sleepDurationProvider: attempt =>
                   {
                       var seconds = 1.0 * Math.Pow(2, attempt - 1); // Exponential back-off; 1s, 2s, 4s, 8s, 16s, 32s, etc...
                       return TimeSpan.FromSeconds(Math.Min(seconds, 30)); // Clamp wait to 50s max
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

                       if (_logger != null)
                       {
                           _logger.LogError(exception, "Blob Retry exception, {message}", exception?.Message);
                       }
                   });
        }

        /// <inheritdoc/>
        public IsPolicy ResilientPolicy { get; }
    }

    /// <summary>
    /// Blob Storage Resiliency Policy Options.
    /// </summary>
    public class BlobStorageResiliencyPolicyOptions
    {
        /// <summary>
        /// Gets or sets maximum number of retries.
        /// </summary>
        public int MaxRetries { get; set; } = 5;
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType