namespace Exos.Platform.AspNetCore.Resiliency.Policies
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;

    /// <summary>
    /// Configure HTTP request policy.
    /// </summary>
    public class HttpRequestPolicyOptions
    {
        private HashSet<int> _retryStatusCode;

        private HashSet<HttpMethod> _retryMethods;

        /// <summary>
        /// Gets or sets a value indicating whether policy should be disabled.
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Gets or sets the retry status code string.
        /// </summary>
        public string RetryStatusCodeString { get; set; }

        /// <summary>
        /// Gets or sets the retry http methods string.
        /// </summary>
        public string RetryMethodString { get; set; }

        /// <summary>
        /// Gets the HTTP codes to attempt a retry.
        /// </summary>
        public HashSet<int> RetryStatusCode
        {
            get => _retryStatusCode ??
                (_retryStatusCode = string.IsNullOrWhiteSpace(RetryStatusCodeString)
                ? new HashSet<int>()
                : RetryStatusCodeString.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => Convert.ToInt32(s, CultureInfo.InvariantCulture))
                .ToHashSet());
        }

        /// <summary>
        /// Gets the HTTP method to which retry should be applied.
        /// Values should be the string representation of HttpMethod defined in`System.Net.Http.HttpMethod` class.
        /// </summary>
        public HashSet<HttpMethod> RetryHttpMethod
        {
            get => _retryMethods ??= string.IsNullOrWhiteSpace(RetryMethodString)
                ? new HashSet<HttpMethod>()
                : RetryMethodString.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(method => new HttpMethod(method.Trim().ToUpperInvariant()))
                .ToHashSet();
        }

        /// <summary>
        /// Gets or sets the number of attempts to retry.
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the length of time that a System.Net.Http.HttpMessageHandler
        /// instance can be reused.
        /// </summary>
        public int HandlerLifetimeInMinutes { get; set; }
    }
}