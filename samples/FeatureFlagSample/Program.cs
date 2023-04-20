#pragma warning disable SA1600 // Elements should be documented

using Exos.Platform;
using Exos.Platform.AspNetCore.Extensions;

namespace FeatureFlagSample
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ExosProgram.LogUnhandledExceptions();
            ExosProgram.HookConsole();

            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UsePlatformConfigurationDefaults();
                    webBuilder.UsePlatformLoggingDefaults();
                    webBuilder.UseStartup<Startup>();
                });
    }
}