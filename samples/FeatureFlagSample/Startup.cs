#pragma warning disable SA1600 // Elements should be documented

using Exos.Platform.AspNetCore.Extensions;
using Microsoft.OpenApi.Models;

namespace FeatureFlagSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddExosPlatformDefaults(Configuration, Environment);
            // Exos.Platform.AspNetCore.Helpers.ConfigurationHelper.DumpToCsvFile(Configuration, "FeatureFlagSample.appsettings.sandbox.csv", includeExosMacros: true);

            services.AddControllers();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
