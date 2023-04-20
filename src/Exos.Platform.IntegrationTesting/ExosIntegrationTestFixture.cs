using Exos.Platform.AspNetCore.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Exos.Platform.IntegrationTesting
{
    /// <inheritdoc/>
    public class ExosIntegrationTestFixture<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder?.ConfigureServices(services =>
            {
                // Remove authentication/authorization for testing
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthorizationHandler) && d.ImplementationType == typeof(ApiResourceAuthorizationHandler));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IAuthorizationHandler, AllowAnonymousAuthorizationFilter>();
            });
        }
    }
}