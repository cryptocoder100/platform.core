namespace Exos.Platform.Persistence.Policies
{
    /// <summary>
    /// SqlRetryPolicyOptions.
    /// </summary>
    public class SqlRetryPolicyOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of retries for a connection..
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public int MaxRetryDelay { get; set; } = 15;

        /// <summary>
        /// Gets or sets the wait time (in seconds) before terminating
        /// the attempt to execute a command.
        /// </summary>
        public int CommandTimeout { get; set; } = 30;
    }
}
