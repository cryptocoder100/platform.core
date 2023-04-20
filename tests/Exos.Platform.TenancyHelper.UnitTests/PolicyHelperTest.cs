using System.Diagnostics.CodeAnalysis;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.TenancyHelper.Interfaces;
using Exos.Platform.TenancyHelper.MultiTenancy;
using Exos.Platform.TenancyHelper.PersistenceService;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Exos.Platform.TenancyHelper.UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class PolicyHelperTest
    {
        private static Mock<IDocumentClientAccessor> _documentClientAccessor;
        private static Mock<IMemoryCache> _memoryCache;
        private static Mock<ILogger<PolicyHelper>> _logger;
        private static Mock<IUserContextService> _userContextService;
        private static Mock<IOptions<PlatformDefaultsOptions>> _platformDefaultsOptions;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _documentClientAccessor = new Mock<IDocumentClientAccessor>();
            _memoryCache = new Mock<IMemoryCache>();
            _logger = new Mock<ILogger<PolicyHelper>>();
            _userContextService = new Mock<IUserContextService>();
            _platformDefaultsOptions = new Mock<IOptions<PlatformDefaultsOptions>>();

            _platformDefaultsOptions.Setup(mock => mock.Value).Returns(new PlatformDefaultsOptions());
        }

        [TestMethod]
        public void TestGetSQLWhereClauseMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("masterclient_user");
            _userContextService.Setup(mock => mock.UserId).Returns("masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
        }

        [TestMethod]
        public void TestGetSQLWhereClauseForSearchesMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.UserId).Returns("masterclient_user");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
        }

        [TestMethod]
        public void TestGetSQLWhereClauseRestrictedMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("restricted_masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.SubTenantType).Returns("restrictedclient");
            _userContextService.Setup(mock => mock.UserId).Returns("restricted_masterclient_user");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 1, 100 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.SubTenantType).Returns(string.Empty);
        }

        [TestMethod]
        public void TestGetSQLWhereClauseForSearchesRestrictedMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("restricted_masterclient_user");
            _userContextService.Setup(mock => mock.UserId).Returns("restricted_masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.SubTenantType).Returns("restrictedclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 1, 100 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 3, 4 });
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubTenantType).Returns(string.Empty);
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.SubTenantType).Returns(string.Empty);
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 1, 100 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"MasterClient Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"MasterClient Cosmos Tenancy IsRelationshipEntity Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseForSearchesMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.SubTenantType).Returns(string.Empty);
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 1, 100 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"MasterClient Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"MasterClient Cosmos Tenancy IsRelationshipEntity Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseRestrictedMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("restricted_masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.SubTenantType).Returns("restrictedclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Restricted MasterClient Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Restricted MasterClient Cosmos Tenancy IsRelationshipEntity Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Restricted MasterClient Cosmos Tenancy IsRelationshipEntity, No SubClient Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseForSearchesRestrictedMasterClient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("restricted_masterclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("masterclient");
            _userContextService.Setup(mock => mock.SubTenantType).Returns("restrictedclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long> { 2008283, 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Restricted MasterClient Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Restricted MasterClient Cosmos Tenancy IsRelationshipEntity Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            _userContextService.Setup(mock => mock.SubClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Restricted MasterClient Cosmos Tenancy IsRelationshipEntity, No SubClient Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Restricted MasterClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseSubclient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"SubClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseForSearchesSubclient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"SubClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubClient SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseSubclient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubClient Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"SubClient Cosmos Tenancy IsRelationshipEntity Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseForSearchesSubclient()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subclient_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subclient");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.MasterClientIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubClient Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"SubClient Cosmos Tenancy IsRelationshipEntity Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseVendor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("vendor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("vendor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.SubVendorIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Vendor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"Vendor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubVendorIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Vendor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseForSearchesVendor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("vendor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("vendor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.SubVendorIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Vendor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"Vendor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.SubVendorIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Vendor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseVendor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("vendor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("vendor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.SubVendorIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Vendor Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Vendor IsRelationshipEntity Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            entityPolicyAttributes.ApplyServicerFilterForVendorTenant = true;
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Vendor IsRelationshipEntity, ApplyServicerFilterForVendorTenant = true, Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Vendor IsRelationshipEntity, ApplyServicerFilterForVendorTenant = true, Non ServiceLink Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseForSearchesVendor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("vendor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("vendor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.SubVendorIds).Returns(new List<long> { 3008285, 3008286 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Vendor Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Vendor IsRelationshipEntity Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            entityPolicyAttributes.ApplyServicerFilterForVendorTenant = true;
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Vendor IsRelationshipEntity, ApplyServicerFilterForVendorTenant = true, Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"Vendor IsRelationshipEntity, ApplyServicerFilterForVendorTenant = true, Non ServiceLink Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseSubContractor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subcontractor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subcontractor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.VendorIds).Returns(new List<long> { 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubContractor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"SubContractor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.VendorIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubContractor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseForSearchesSubContractor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subcontractor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subcontractor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.VendorIds).Returns(new List<long> { 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubContractor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" }, new List<string> { "WorkOrder.WorkOrderId=1" }, "WorkOrderAlias");
            Console.WriteLine($"SubContractor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.VendorIds).Returns(new List<long>());
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"SubContractor SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseSubContractor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subcontractor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subcontractor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.VendorIds).Returns(new List<long> { 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true,
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubContractor Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            entityPolicyAttributes.ApplyServicerFilterForVendorTenant = true;
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubContractor ApplyServicerFilterForVendorTenant = true  Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubContractor ApplyServicerFilterForVendorTenant = true, No ServiceLink Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"SubContractor IsRelationshipEntity , ApplyServicerFilterForVendorTenant = true, No ServiceLink Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseForSearchesSubContractor()
        {
            _userContextService.Setup(mock => mock.Username).Returns("subcontractor_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("subcontractor");
            _userContextService.Setup(mock => mock.TenantId).Returns(2008286);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.VendorIds).Returns(new List<long> { 2008284 });
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true,
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubContractor Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            entityPolicyAttributes.ApplyServicerFilterForVendorTenant = true;
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubContractor ApplyServicerFilterForVendorTenant = true  Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"SubContractor ApplyServicerFilterForVendorTenant = true, No ServiceLink Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection { new SqlParameter("@cosmosDocType", "VendorProfile") }, entityPolicyAttributes);
            Console.WriteLine($"SubContractor IsRelationshipEntity , ApplyServicerFilterForVendorTenant = true, No ServiceLink Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseServicer()
        {
            _userContextService.Setup(mock => mock.Username).Returns("servicer_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("servicer");
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.GetUserContext()).Returns(new Mock<IUserContext>().Object);
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 7, 8 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerGroups).Returns(new List<long> { 4, 5, 6 });
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" }, new List<string> { " AND WorkOrder.WorkOrderId = 1" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Additional Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.GetUserContext().ServicerGroupTenantId).Returns(14);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.GetUserContext().ServicerGroupTenantId).Returns(0);
            _userContextService.Setup(mock => mock.ServicerGroups).Returns(new List<long>());
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClause(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetSQLWhereClauseForSearchesServicer()
        {
            _userContextService.Setup(mock => mock.Username).Returns("servicer_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("servicer");
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.GetUserContext()).Returns(new Mock<IUserContext>().Object);
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 7, 8 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            var tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.ServicerGroups).Returns(new List<long> { 4, 5, 6 });
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" }, new List<string> { " AND WorkOrder.WorkOrderId = 1" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Additional Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.GetUserContext().ServicerGroupTenantId).Returns(14);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.GetUserContext().ServicerGroupTenantId).Returns(0);
            _userContextService.Setup(mock => mock.ServicerGroups).Returns(new List<long>());
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetSQLWhereClauseForSearches(new List<string> { "WorkOrder" });
            Console.WriteLine($"Servicer 100 SQL Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseServicer()
        {
            _userContextService.Setup(mock => mock.Username).Returns("servicer_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("servicer");
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 7, 8, 9 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetCosmosWhereClause("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Servicer 100 (No ServiceLink) Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long>());
        }

        [TestMethod]
        public void TestGetCosmosWhereClauseForSearchesServicer()
        {
            _userContextService.Setup(mock => mock.Username).Returns("servicer_user");
            _userContextService.Setup(mock => mock.TenantType).Returns("servicer");
            _userContextService.Setup(mock => mock.TenantId).Returns(1);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(1);
            _userContextService.Setup(mock => mock.LinesOfBusiness).Returns(new List<long> { 3 });
            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long> { 7, 8, 9 });

            var policyHelper = new PolicyHelper(_documentClientAccessor.Object, _memoryCache.Object, _logger.Object, _userContextService.Object, _platformDefaultsOptions.Object);
            EntityPolicyAttributes entityPolicyAttributes = new EntityPolicyAttributes
            {
                IsEntityMultiTenant = true
            };
            var tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Servicer Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            _userContextService.Setup(mock => mock.TenantId).Returns(100);
            _userContextService.Setup(mock => mock.ServicerTenantId).Returns(100);
            tenancyWhereCondition = policyHelper.GetCosmosWhereClauseForSearches("c.", new SqlParameterCollection(), entityPolicyAttributes);
            Console.WriteLine($"Servicer 100 (No ServiceLink) Cosmos Tenancy Where Condition:{tenancyWhereCondition}");
            Assert.IsFalse(string.IsNullOrEmpty(tenancyWhereCondition));

            _userContextService.Setup(mock => mock.AssociatedServicerTenantIds).Returns(new List<long>());
        }
    }
}
