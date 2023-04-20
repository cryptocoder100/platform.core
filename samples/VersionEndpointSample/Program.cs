#pragma warning disable SA1600 // Elements should be documented

using Exos.Platform;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace VersionEndpointSample
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ExosProgram.LogUnhandledExceptions();
            ExosProgram.HookConsole();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
#pragma warning restore SA1600 // Elements should be documented