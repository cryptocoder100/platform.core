namespace Exos.Platform.AspNetCore
{
    using System;
    using System.Threading;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class ThreadPoolService : IThreadPoolService
    {
        private readonly ILogger<ThreadPoolService> _logger;
        private readonly ThreadPoolOptions _threadPoolOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadPoolService"/> class.
        /// </summary>
        /// <param name="logger">The logger<see cref="Logger{ThreadPoolService}"/>.</param>
        /// <param name="threadPoolOptions">The threadPoolOptions<see cref="IOptions{ThreadPoolOptions}"/>.</param>
        public ThreadPoolService(ILogger<ThreadPoolService> logger, IOptions<ThreadPoolOptions> threadPoolOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _threadPoolOptions = threadPoolOptions?.Value ?? throw new ArgumentNullException(nameof(threadPoolOptions));

            if (_threadPoolOptions != null)
            {
                if (int.TryParse(_threadPoolOptions.MinWorkerThreads, out int minWThread) &&
                    int.TryParse(_threadPoolOptions.MinCompletionPortThreads, out int minIoThread) &&
                    minWThread > 0 && minIoThread > 0)
                {
                    SetMinThreads(new ThreadPoolConfig { MinWorkerThreads = minWThread, MinCompletionPortThreads = minIoThread });
                }
            }
        }

        /// <inheritdoc/>
        public ThreadPoolInfo GetThreadPoolInfo()
        {
            ThreadPoolInfo response = GetThreadPoolInfoInternal();
            return response;
        }

        /// <inheritdoc/>
        public ThreadPoolInfo SetMinThreads(ThreadPoolConfig threadConfig)
        {
            if (threadConfig == null)
            {
                throw new ArgumentNullException(nameof(threadConfig));
            }

            ThreadPoolInfo response = GetThreadPoolInfoInternal();

            // Set the min threads to what we have if user didn't pass the values.
            threadConfig.MinWorkerThreads ??= response.MinWorkerThreads;
            threadConfig.MinCompletionPortThreads ??= response.MinCompletionPortThreads;

            _logger.LogDebug($"User Request: Min(WT) - {threadConfig.MinWorkerThreads}, Min(IOC) - {threadConfig.MinCompletionPortThreads}");

            ThreadPool.SetMinThreads(threadConfig.MinWorkerThreads.Value, threadConfig.MinCompletionPortThreads.Value);

            // Get the updated info
            response = GetThreadPoolInfo();

            return response;
        }

        /// <summary>
        /// Get Thread Pool details.
        /// </summary>
        /// <returns>Managed thread pool information.</returns>
        private ThreadPoolInfo GetThreadPoolInfoInternal()
        {
            // Get the current thread pool information and return.
            ThreadPoolInfo response = new ThreadPoolInfo();
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIoThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);
            ThreadPool.GetAvailableThreads(out int avlWorkerThreads, out int avlIoThreads);

            response.MinWorkerThreads = minWorkerThreads;
            response.MinCompletionPortThreads = minIoThreads;
            response.MaxWorkerThreads = maxWorkerThreads;
            response.MaxCompletionPortThreads = maxIoThreads;
            response.AvailableWorkerThreads = avlWorkerThreads;
            response.AvailableCompletionPortThreads = avlIoThreads;

            _logger.LogDebug($".NET Thread Pool Information: Min(WT) - {minWorkerThreads}, Min(IOC) - {minIoThreads}, Max(WT) - {maxWorkerThreads}, Max(IOC) - {maxIoThreads}, Avl(WT) - {avlWorkerThreads}, Avl(IOC) - {avlIoThreads}");
            return response;
        }
    }
}
