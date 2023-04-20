namespace Exos.Platform.AspNetCore
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Configure managed thread pool.
    /// </summary>
    public interface IThreadPoolService
    {
        /// <summary>
        /// Get Thread Pool details.
        /// </summary>
        /// <returns>Managed thread pool information.</returns>
        ThreadPoolInfo GetThreadPoolInfo();

        /// <summary>
        /// Sets the minimum I/O completion thread count.
        /// </summary>
        /// <param name="threadConfig">Thread Pool Configuration.</param>
        /// <returns>Managed thread pool information.</returns>
        ThreadPoolInfo SetMinThreads(ThreadPoolConfig threadConfig);
    }
}
