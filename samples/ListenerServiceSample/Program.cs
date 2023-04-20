using Exos.Platform;
using Exos.Platform.AspNetCore.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ListenerServiceSample
{
    /// <summary>
    /// Entry Point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry Point.
        /// </summary>
        /// <param name="args">Command line parameters.</param>
        public static void Main(string[] args)
        {
            ExosProgram.LogUnhandledExceptions();
            ExosProgram.HookConsole();

            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Create host builder.
        /// </summary>
        /// <param name="args">Command line parameters.</param>
        /// <returns>Host builder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UsePlatformConfigurationDefaults();
                    webBuilder.UsePlatformLoggingDefaults();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
