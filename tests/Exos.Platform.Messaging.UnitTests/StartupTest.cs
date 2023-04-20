using System.Diagnostics.CodeAnalysis;

namespace AzureMessagingTest
{
    [ExcludeFromCodeCoverage]
    public class StartupTest
    {
        // public StartupTest(IHostingEnvironment env)
        // {
        //    var builder = new ConfigurationBuilder()
        //        .SetBasePath(env.ContentRootPath)
        //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        //        .AddEnvironmentVariables();
        //    Configuration = builder.Build();
        // }

        // public IConfigurationRoot Configuration { get; }

        //// This method gets called by the runtime. Use this method to add services to the container.
        // public void ConfigureServices(IServiceCollection services)
        // {
        //    // Add framework services.
        //    services.Configure<MessageSection>(Configuration.GetSection("Messaging"));
        //    var config = Configuration.GetSection("Messaging");
        //    services.AddSingleton<IMessagingRepository, MessagingRepository>();
        //    services.AddOptions();
        // }

        //// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // public void Configure(IApplicationBuilder app,
        //    IHostingEnvironment env,
        //    Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        // {
        //    loggerFactory.AddConsole(Configuration.GetSection("Logging"));
        //    loggerFactory.AddDebug();

        // app.UseMvc();
        // }

        // public static IMessagingRepository TestMethod()
        // {
        //    return null;
        // }
    }
}