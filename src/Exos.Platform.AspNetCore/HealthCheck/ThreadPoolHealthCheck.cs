#pragma warning disable CA1031 // Do not catch general exception types

namespace Exos.Platform.AspNetCore.HealthCheck
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="ThreadPoolHealthCheck" />.
    /// </summary>
    public class ThreadPoolHealthCheck : IHealthCheck
    {
        private readonly ILogger _logger;
        private readonly IThreadPoolService _threadPoolService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadPoolHealthCheck"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="ILogger{ThreadPoolHealtchCheck}"/>.</param>
        /// <param name="threadPoolService">The threadPoolService<see cref="IThreadPoolService"/>.</param>
        public ThreadPoolHealthCheck(ILogger<ThreadPoolHealthCheck> logger, IThreadPoolService threadPoolService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _threadPoolService = threadPoolService ?? throw new ArgumentNullException(nameof(threadPoolService));
        }

        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _threadPoolService.GetThreadPoolInfo();
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(HealthCheckResult.Unhealthy());
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types