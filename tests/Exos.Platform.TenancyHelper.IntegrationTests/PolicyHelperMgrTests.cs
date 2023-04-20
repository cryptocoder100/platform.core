using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.TenancyHelper.IntegrationTests.Helpers;
using Exos.Platform.TenancyHelper.Interfaces;
using Exos.Platform.TenancyHelper.MultiTenancy;
using Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl;
using Exos.Platform.TenancyHelper.PersistenceService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Exos.Platform.TenancyHelper.IntegrationTests
{
    [ExcludeFromCodeCoverage]
    public class PolicyHelperMgrTests
    {
        [Fact]
        public async Task GetPolicyObject_WithTenantId_ReturnsPolicyDocument()
        {
            // Arrange
            var builder = HostHelper.CreateWebHostDefault();
            builder.ConfigureServices((ctx, services) =>
            {
                services.Configure<RepositoryOptions>(ctx.Configuration.GetSection("Cosmos:AdminRepositoryOptions"));
                services.AddScoped<IDocumentClientAccessor, DocumentClientAccessor>();
                services.AddScoped<ILogger>(sp => sp.GetRequiredService<ILogger<PolicyHelper>>());
                services.AddScoped<PolicyHelperMgr>();
            });

            var host = builder.Build();
            using var scope = host.Services.CreateScope();
            var policyMgr = scope.ServiceProvider.GetService<PolicyHelperMgr>();

            // Act
            var policy = await policyMgr.GetPolicyObject(new
            {
                CosmosDocType = "CountyZipCode"
            });

            var obj = policy as JObject;

            // Assert
            Assert.IsType<JObject>(policy);
            Assert.Equal("CountyZipCodePolicyDocument", (string)obj["cosmosDocType"]);
        }
    }
}
