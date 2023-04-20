using System.Diagnostics.CodeAnalysis;
using Exos.Platform.AspNetCore.Middleware;
using Exos.Platform.AspNetCore.Models;
using Exos.Platform.AspNetCore.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exos.Platform.AspNetCore.UnitTests.Security
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class VendorAuthorizationActionFilterTests
    {
        private static ServiceProvider _serviceProvider;
        private static ILogger<VendorAuthorizationActionFilterTests> _logger;

        /// <summary>
        /// Executes once for the test class.
        /// </summary>
        /// <param name="testContext"><see cref="TestContext"/>.</param>
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            IServiceCollection serviceCollection = new ServiceCollection()
                .AddLogging(options => options.AddConsole())
                .AddScoped<IUserContextAccessor, UserContextAccessor>()
                .AddScoped<IHttpContextAccessor, MockHttpContextAccessor>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _logger = _serviceProvider.GetService<ILogger<VendorAuthorizationActionFilterTests>>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_WithNullArgument_ShouldThrowNullArgumentException()
        {
            new VendorAuthorizationActionFilter(null);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public void InvalidVendorUser_ShouldThrowUnauthorizedException()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.vendor.ToString();
            userContextAccessor.TenantId = 12345678;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            filter.OnActionExecuting(GetActionExecutingContext(GetActionArguments()));
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public void InvalidSubcontractrorUser_ShouldThrowUnauthorizedException()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.subcontractor.ToString();
            userContextAccessor.TenantId = 12345678;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            filter.OnActionExecuting(GetActionExecutingContext(GetActionArguments()));
        }

        [TestMethod]
        public void ValidVendorUser_ThrowNoException()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.vendor.ToString();
            userContextAccessor.TenantId = 90345678;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            filter.OnActionExecuting(GetActionExecutingContext(GetActionArguments()));
        }

        [TestMethod]
        public void ValidVendorUser_PayloadNull()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.vendor.ToString();
            userContextAccessor.TenantId = 90345678;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            IDictionary<string, object> actionArguments = new Dictionary<string, object>
            {
                { "payload", null }
            };
            filter.OnActionExecuting(GetActionExecutingContext(actionArguments));
        }

        [TestMethod]
        public void ValidVendorUser_ActionArgumentsEmpty()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.vendor.ToString();
            userContextAccessor.TenantId = 90345678;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            IDictionary<string, object> actionArguments = new Dictionary<string, object>();
            filter.OnActionExecuting(GetActionExecutingContext(actionArguments));
        }

        [TestMethod]
        public void ValidSubcontractorUser_ThrowNoException()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.subcontractor.ToString();
            userContextAccessor.TenantId = 98115863;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            filter.OnActionExecuting(GetActionExecutingContext(GetActionArguments()));
        }

        [TestMethod]
        public void ValidServicerUser_ThrowNoException()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.servicer.ToString();
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            filter.OnActionExecuting(GetActionExecutingContext(GetActionArguments()));
        }

        [TestMethod]
        public void ValidVendorUser_PayloadNotContainsIds()
        {
            var userContextAccessor = _serviceProvider.GetService<IUserContextAccessor>();
            userContextAccessor.TenantType = TenantTypes.vendor.ToString();
            userContextAccessor.TenantId = 90345678;
            VendorAuthorizationActionFilter filter = new VendorAuthorizationActionFilter(userContextAccessor);
            IDictionary<string, object> actionArguments = new Dictionary<string, object>();
            var payload = new
            {
                x1 = 90345678,
                x2 = 98115863,
            };
            actionArguments.Add("payload", payload);
            filter.OnActionExecuting(GetActionExecutingContext(actionArguments));
        }

        private static IDictionary<string, object> GetActionArguments()
        {
            var payload = new
            {
                VendorId = 90345678,
                SubContractorId = 98115863,
            };
            IDictionary<string, object> actionArguments = new Dictionary<string, object>
            {
                { "payload", payload }
            };
            return actionArguments;
        }

        private static ActionExecutingContext GetActionExecutingContext(IDictionary<string, object> actionArguments)
        {
            var context = _serviceProvider.GetService<IHttpContextAccessor>();
            ActionContext actionContext = new ActionContext
            {
                HttpContext = context.HttpContext,
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            };
            IList<IFilterMetadata> list = new List<IFilterMetadata>();
            ActionExecutingContext actionExecutingContext = new ActionExecutingContext(actionContext, list, actionArguments, null);
            return actionExecutingContext;
        }
    }
}
