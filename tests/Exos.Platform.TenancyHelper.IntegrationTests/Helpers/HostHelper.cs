using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.TenancyHelper.Interfaces;
using Exos.Platform.TenancyHelper.MultiTenancy;
using Exos.Platform.TenancyHelper.PersistenceService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Exos.Platform.TenancyHelper.IntegrationTests.Helpers
{
    [ExcludeFromCodeCoverage]
    internal static class HostHelper
    {
        public static IHostBuilder CreateWebHostDefault()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureServices((ctx, services) =>
                    {
                        services.AddExosPlatformDefaults(ctx.Configuration, ctx.HostingEnvironment);

                        services.AddScoped<IUserContextService, UserContextService>();
                        services.AddScoped<IUserContextAccessor, UserContextAccessor>();

                        services.AddExosRedisConnectionPool(options =>
                        {
                            options.Configuration = ctx.Configuration.GetValue<string>("Redis:ReadWriteConnectionString");
                            options.PoolSize = ctx.Configuration.GetValue("Redis:PoolSize", 3);
                        });
                        services.AddExosRedisDistributedCache(options =>
                        {
                            options.MaxRetries = ctx.Configuration.GetValue("Redis:MaxRetries", 6);
                        });
                    });
                });

            return builder;
        }
    }
}
