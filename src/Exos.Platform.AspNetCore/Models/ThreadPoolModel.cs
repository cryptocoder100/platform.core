#pragma warning disable SA1402 // FileMayOnlyContainASingleType
#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName

namespace Exos.Platform.AspNetCore.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Thread Pool configuration options.
    /// </summary>
    public class ThreadPoolOptions
    {
        /// <summary>
        /// Gets or sets minimum worker thread count.
        /// </summary>
        public string MinWorkerThreads { get; set; }

        /// <summary>
        /// Gets or sets minimum I/O completion thread count.
        /// </summary>
        public string MinCompletionPortThreads { get; set; }
    }

    /// <summary>
    /// Request object to set the minimum number of threads in .NET managed thread pool.
    /// </summary>
    public class ThreadPoolConfig
    {
        /// <summary>
        /// Gets or sets minimum worker thread count.
        /// </summary>
        [Range(2, 300, ErrorMessage = "Worker Thread count should be between 2 and 300 range.")]
        public int? MinWorkerThreads { get; set; }

        /// <summary>
        /// Gets or sets minimum I/O completion thread count.
        /// </summary>
        [Range(2, 300, ErrorMessage = "Completion Port Thread count should be between 2 and 300 range.")]
        public int? MinCompletionPortThreads { get; set; }
    }

    /// <summary>
    /// .NET managed thread pool information.
    /// </summary>
    public class ThreadPoolInfo
    {
        /// <summary>
        /// Gets or sets minimum worker thread count.
        /// </summary>
        public int MinWorkerThreads { get; set; }

        /// <summary>
        /// Gets or sets maximum worker thread count.
        /// </summary>
        public int MaxWorkerThreads { get; set; }

        /// <summary>
        /// Gets or sets minimum I/O completion thread count.
        /// </summary>
        public int MinCompletionPortThreads { get; set; }

        /// <summary>
        /// Gets or sets maximum I/O completion thread count.
        /// </summary>
        public int MaxCompletionPortThreads { get; set; }

        /// <summary>
        /// Gets or sets available worker thread count.
        /// </summary>
        public int AvailableWorkerThreads { get; set; }

        /// <summary>
        /// Gets or sets available I/O completion thread count.
        /// </summary>
        public int AvailableCompletionPortThreads { get; set; }
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName