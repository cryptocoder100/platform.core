#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.AspNetCore.IntegrationTests
{
    using System.Net.Http;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.IntegrationTests.Mock;
    using Exos.Platform.AspNetCore.IntegrationTests.Options;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class HttpContextClientFactoryStartup
    {
        public HttpContextClientFactoryStartup(IConfiguration config, IWebHostEnvironment env)
        {
            Configuration = config;
            Environment = env;
        }

        public static IConfiguration Configuration { get; set; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // platform dependencies
            services.Configure<UserContextOptions>(Configuration.GetSection("JWT"));
            services.TryAddSingleton<IHttpContextAccessor, MockHttpContextAccessor>();
            services.AddExosPlatformDefaults(Configuration, Environment);

            services.AddHttpClient();
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddSingleton<HttpClient>(HttpContextClientFactory.Create);
#pragma warning restore CS0618 // Type or member is obsolete

            // add external dependencies
            services.Configure<ExternalServiceOptions>(Configuration.GetSection("ExternalServiceOptions"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore CA1822 // Mark members as static

