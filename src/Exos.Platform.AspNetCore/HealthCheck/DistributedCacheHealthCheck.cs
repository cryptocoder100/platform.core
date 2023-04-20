#pragma warning disable CA1031 // Do not catch general exception types

namespace Exos.Platform.AspNetCore.HealthCheck
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc/>
    public class DistributedCacheHealthCheck : IHealthCheck
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedCacheHealthCheck"/> class.
        /// </summary>
        /// <param name="cache"><see cref="IDistributedCache"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        public DistributedCacheHealthCheck(IDistributedCache cache, ILogger<DistributedCacheHealthCheck> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // Attempt to get a cache entry (regardless whether it exists) to
                // determine we have a healthy connection
                await _cache.GetAsync("HealthCheck", cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return HealthCheckResult.Unhealthy();
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types