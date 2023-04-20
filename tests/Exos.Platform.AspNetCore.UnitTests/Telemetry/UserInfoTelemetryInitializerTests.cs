using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using Exos.Platform.AspNetCore.Middleware;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.AspNetCore.Telemetry;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Exos.Platform.AspNetCore.UnitTests.Telemetry
{
    [TestClass]
    public class UserInfoTelemetryInitializerTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_WithNullArgument_ShouldThrowNullArgumentException()
        {
            new UserInfoTelemetryInitializer(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Initialize_WithNullArgument_ShouldThrowNullArgumentException()
        {
            var initializer = new UserInfoTelemetryInitializer(new HttpContextAccessor());
            initializer.Initialize(null);
        }

        [TestMethod]
        public void Initialze_WithoutHttpContext_ShouldFailGracefully()
        {
            var initializer = new UserInfoTelemetryInitializer(new HttpContextAccessor());
            var telemtry = new TraceTelemetry();
            initializer.Initialize(telemtry);

            Assert.IsFalse(telemtry.Properties.ContainsKey("UserId"));
            Assert.IsFalse(telemtry.Properties.ContainsKey("UserIsInternal"));
            Assert.IsFalse(telemtry.Properties.ContainsKey("UserIsAdmin"));
        }

        [TestMethod]
        public void Initialze_WithoutHttpContextUserClaims_ShouldFailGracefully()
        {
            var context = new HttpContextAccessor();
            context.HttpContext = new DefaultHttpContext();
            context.HttpContext.User = new System.Security.Claims.ClaimsPrincipal();

            var initializer = new UserInfoTelemetryInitializer(context);
            var telemtry = new TraceTelemetry();
            initializer.Initialize(telemtry);

            Assert.IsFalse(telemtry.Properties.ContainsKey("UserId"));
            Assert.IsFalse(telemtry.Properties.ContainsKey("UserIsInternal"));
            Assert.IsFalse(telemtry.Properties.ContainsKey("UserIsAdmin"));
        }

        [TestMethod]
        public void Initialze_WithHttpContext_ShouldAddUserInfo()
        {
            var context = new HttpContextAccessor();
            context.HttpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "abc"),
                new Claim(ClaimTypes.Name, "abc@svclnk.com"),
                new Claim(ExosClaimTypes.ExosAdmin, "True")
            };
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var initializer = new UserInfoTelemetryInitializer(context);
            var telemtry = new TraceTelemetry();
            initializer.Initialize(telemtry);

            Assert.AreEqual("abc", telemtry.Properties["UserId"]);
            Assert.AreEqual("true", telemtry.Properties["UserIsInternal"]);
            Assert.AreEqual("true", telemtry.Properties["UserIsAdmin"]);
        }
    }
}
