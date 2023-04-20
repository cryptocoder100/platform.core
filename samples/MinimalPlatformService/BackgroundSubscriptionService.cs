namespace Exos.MinimalPlatformService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.MinimalPlatformService.Models;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An example background service that executes its business logic in a polling loop.
    /// </summary>
    public class BackgroundSubscriptionService : BackgroundService
    {
        private readonly StatusModel _status;
        private readonly ILogger<BackgroundPollingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundSubscriptionService"/> class.
        /// </summary>
        /// <param name="status">StatusModel.</param>
        /// <param name="logger">Logger instance.</param>
        public BackgroundSubscriptionService(StatusModel status, ILogger<BackgroundPollingService> logger)
        {
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Starting
            _logger.LogInformation("Background subscription service starting");
            _status.SubscriptionService.Status = Status.Starting;

            // For this sample we are using a timer to simulate a subscription with a callback
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);    // Simulate taking some time starting
            var subscriptionTimer = new Timer(LogicCallback, stoppingToken, 20000, 20000);

            // Running
            _status.SubscriptionService.Started = DateTimeOffset.UtcNow;
            _status.SubscriptionService.Status = Status.Running;
            _logger.LogInformation("Background subscription service running");

            // Wait for the cancellation token to be set
            try
            {
                await Task.Delay(-1, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }

            // Stopping
            _logger.LogInformation("Background subscription service stopping");
            _status.SubscriptionService.Status = Status.Stopping;
            _status.SubscriptionService.Started = default(DateTimeOffset?);

            subscriptionTimer.Dispose();
            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);     // Simulate taking some time stopping

            // Stopped
            _status.SubscriptionService.Status = Status.Stopped;
            _logger.LogInformation("Background subscription service stopped");
        }

        private void LogicCallback(object state)
        {
            _logger.LogInformation("Background subscription logic callback");
            _status.SubscriptionService.Actions++;

            // Service logic goes here

            // Simulate the process taking some time
            var cancellationToken = (CancellationToken)state;
            try
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).Wait();
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
