#pragma warning disable CA2000 // _factory is static object, will be disposed during application shutown.
namespace Exos.Platform.Messaging.Helper
{
    using Exos.Platform.Messaging.Core;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Based on the suggestion from stackify blog.
    /// </summary>
    public static class MessagingLibraryLogging
    {
        private static ILoggerFactory _factory;
        private static ILogger _iLogger;

        /// <summary>
        /// Gets the Logger.
        /// </summary>
        public static ILogger Logger
        {
            get
            {
                if (_iLogger == null)
                {
                    ConfigureLogger(null, null);
                }

                return _iLogger;
            }
        }

        /// <summary>
        /// Configure Logger Instance.
        /// </summary>
        /// <param name="factory">ILoggerFactory.</param>
        /// <param name="logger">ILogger.</param>
        public static void ConfigureLogger(ILoggerFactory factory, ILogger logger)
        {
            // factory.AddDebug(LogLevel.None);
            // factory.AddFile("logFile.log");
            if (factory == null)
            {
                // TODO: initialize from appsettings.json
                _factory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                });

                // .AddFile("c:/logs/b.txt"); //We should not hit this far in.
                _iLogger = _factory.CreateLogger<ExosMessaging>();
            }
            else if (logger == null)
            {
                _iLogger = _factory.CreateLogger<ExosMessaging>();
            }
            else
            {
                _iLogger = logger;
            }
        }
    }
}
#pragma warning restore CA2000 // _factory is static object, will be disposed during application shutown.