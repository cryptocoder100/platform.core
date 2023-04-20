namespace Exos.Platform.Messaging.Core
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines the <see cref="FailoverConfig"/>.
    /// </summary>
    public class FailoverConfig
    {
        private const string _defaultExceptionNamesString =
            "Microsoft.Azure.ServiceBus.QuotaExceededException," +
            "Microsoft.ServiceBus.Messaging.ServerBusyException," +
            "System.TimeoutException";

        private string _exceptionNamesString = _defaultExceptionNamesString;
        private HashSet<string> _exceptionNames = _defaultExceptionNamesString.Split(",").ToHashSet();

        /// <summary>
        /// Gets or sets a value indicating whether isFailoverEnabled.
        /// </summary>
        public bool IsFailoverEnabled { get; set; }

        /// <summary>
        /// Gets or sets the effective duration of the exception counter.
        /// </summary>
        public int SlidingDurationInSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the threshold of the accumulated exceptions.
        /// </summary>
        public int ExceptionThreshold { get; set; } = 99;

        /// <summary>
        /// Gets or sets the fully qualified names of exceptions in comma separated format.
        /// </summary>
        public string ExceptionNamesString
        {
            get
            {
                return _exceptionNamesString;
            }

            set
            {
                _exceptionNamesString = value;
                _exceptionNames.Clear();
                if (!string.IsNullOrWhiteSpace(_exceptionNamesString))
                {
                    _exceptionNames.UnionWith(ExceptionNamesString
                        .Split(",").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToHashSet());
                }
            }
        }

        /// <summary>
        /// Gets the fully qualified name of exceptions that should be captured.
        /// </summary>
        public HashSet<string> ExceptionNames { get => _exceptionNames; }
    }
}