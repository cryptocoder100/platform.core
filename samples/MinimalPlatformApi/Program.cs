namespace Exos.MinimalPlatformApi
{
    using Exos.Platform;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

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

            BuildWebHost(args).Run();
        }

        /// <summary>
        /// Build a web host.
        /// </summary>
        /// <param name="args">Command line parameters.</param>
        /// <returns>A configured web host.</returns>
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)

                // Add the platform default configuration sources before the Startup class is processed
                .UsePlatformConfigurationDefaults()

                // Add the platform default logging providers before the Startup class is processed and
                // after the configuration sources are setup
                .UsePlatformLoggingDefaults()

                .UseStartup<Startup>()
                .Build();
    }
}
