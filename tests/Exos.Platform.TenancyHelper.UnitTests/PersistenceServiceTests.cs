#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Exos.Platform.TenancyHelper.Interfaces;
using Exos.Platform.TenancyHelper.PersistenceService;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Exos.Platform.TenancyHelper.UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class PersistenceServiceTests
    {
        [TestMethod]
        public void GetQuerySpec_SanityCheck()
        {
            var repositoryOptions = new RepositoryOptions { ApplyDocumentPolicy = true };
            var documentClientAccessor = new Mock<IDocumentClientAccessor>();
            documentClientAccessor.Setup(d => d.RepositoryOptions).Returns(repositoryOptions);

            var distributedCache = new Mock<IDistributedCache>();
            var userContextService = new Mock<IUserContextService>();
            var policyHelper = new Mock<IPolicyHelper>();
            var policyContext = new Mock<IPolicyContext>();
            var logger = NullLogger<Exos.Platform.TenancyHelper.PersistenceService.PersistenceService>.Instance;

            var telemetryClient = new TelemetryClient();

            var persistenceService = new Exos.Platform.TenancyHelper.PersistenceService.PersistenceService(
                documentClientAccessor.Object,
                distributedCache.Object,
                userContextService.Object,
                policyHelper.Object,
                policyContext.Object,
                logger,
                telemetryClient);

            using var writer = new StringWriter();
            var querySpec = persistenceService.GetQuerySpec(
                writer,
                new SqlParameterCollection(),
                "abc",
                new MultiTenancy.EntityPolicyAttributes { IsEntityMultiTenant = true },
                orderBy: "Id",
                tenantWhereClausePlaceHolderRef: null);
        }
    }
}
