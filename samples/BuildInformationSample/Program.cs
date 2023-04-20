#pragma warning disable SA1600

using Exos.Platform;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ApplicationInsightsSample
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ExosProgram.LogUnhandledExceptions();
            ExosProgram.HookConsole();

            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
