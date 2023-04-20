namespace Exos.Platform.Messaging.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.Messaging.Core.Extension;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class PublisherTestStartup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public PublisherTestStartup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The app<see cref="IApplicationBuilder"/>.</param>
        public static void Configure(IApplicationBuilder app)
        {
            // Error handling (should be early in sequence)
            app.UseErrorHandler();
            app.RunNotFoundException();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/>.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // EXOS Platform MVC defaults (includes the call to AddMvc/AddControllers)
            services.AddExosPlatformDefaults(_configuration, _environment);

            services.ConfigureAzureServiceBusEntityPublisher(_configuration);
        }
    }
}
