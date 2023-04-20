#pragma warning disable CA1506 // Avoid excessive class coupling
namespace Exos.Platform.AspNetCore.UnitTests.Middleware
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Models;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ErrorHandlerMiddlewareTests
    {
        [TestMethod]
        public async Task Invoke_WithArgumentNullException_ReturnsBadRequest()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IWebHostEnvironment>(new Mock<IWebHostEnvironment>().Object);
            serviceCollection.AddSingleton<IOptions<PlatformDefaultsOptions>>(Options.Create(new PlatformDefaultsOptions()));
            serviceCollection.AddSingleton<ILogger<ErrorHandlerMiddleware>>(NullLogger<ErrorHandlerMiddleware>.Instance);
            serviceCollection.AddSingleton<IOptions<MvcNewtonsoftJsonOptions>>(Options.Create(new MvcNewtonsoftJsonOptions()));
            var requestDelegate = new Mock<RequestDelegate>();
            requestDelegate.Setup(rd => rd.Invoke(It.IsAny<HttpContext>()))
                .Throws(new ArgumentNullException("orderId"));

            using var body = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Response.Body = body;
            var errorHandlerMiddleware = new ErrorHandlerMiddleware(
                requestDelegate.Object,
                serviceCollection.BuildServiceProvider());

            // Act
            await errorHandlerMiddleware.Invoke(context);
            body.Position = 0;
            using var reader = new StreamReader(body);
            var model = JsonSerializer.Deserialize<ErrorModel>(reader.ReadToEnd(), new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            Assert.AreEqual(400, context.Response.StatusCode);
            Assert.AreEqual("application/json", context.Response.ContentType);
            Assert.AreEqual(ErrorType.InvalidRequestError, model.Type);
            Assert.IsTrue(model.Params.Any(e => e.Name == "orderId"));
        }

        [TestMethod]
        public async Task Invoke_WithArgumentOutOfRangeException_ReturnsBadRequest()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IWebHostEnvironment>(new Mock<IWebHostEnvironment>().Object);
            serviceCollection.AddSingleton<IOptions<PlatformDefaultsOptions>>(Options.Create(new PlatformDefaultsOptions()));
            serviceCollection.AddSingleton<ILogger<ErrorHandlerMiddleware>>(NullLogger<ErrorHandlerMiddleware>.Instance);
            serviceCollection.AddSingleton<IOptions<MvcNewtonsoftJsonOptions>>(Options.Create(new MvcNewtonsoftJsonOptions()));
            var requestDelegate = new Mock<RequestDelegate>();
            requestDelegate.Setup(rd => rd.Invoke(It.IsAny<HttpContext>()))
                .Throws(new ArgumentOutOfRangeException("orderId"));

            using var body = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Response.Body = body;
            var errorHandlerMiddleware = new ErrorHandlerMiddleware(
                requestDelegate.Object,
                serviceCollection.BuildServiceProvider());

            // Act
            await errorHandlerMiddleware.Invoke(context);
            body.Position = 0;
            using var reader = new StreamReader(body);
            var model = JsonSerializer.Deserialize<ErrorModel>(reader.ReadToEnd(), new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            Assert.AreEqual(400, context.Response.StatusCode);
            Assert.AreEqual("application/json", context.Response.ContentType);
            Assert.AreEqual(ErrorType.InvalidRequestError, model.Type);
            Assert.IsTrue(model.Params.Any(e => e.Name == "orderId"));
        }

        [TestMethod]
        public async Task Invoke_WithArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IWebHostEnvironment>(new Mock<IWebHostEnvironment>().Object);
            serviceCollection.AddSingleton<IOptions<PlatformDefaultsOptions>>(Options.Create(new PlatformDefaultsOptions()));
            serviceCollection.AddSingleton<ILogger<ErrorHandlerMiddleware>>(NullLogger<ErrorHandlerMiddleware>.Instance);
            serviceCollection.AddSingleton<IOptions<MvcNewtonsoftJsonOptions>>(Options.Create(new MvcNewtonsoftJsonOptions()));
            var requestDelegate = new Mock<RequestDelegate>();
            requestDelegate.Setup(rd => rd.Invoke(It.IsAny<HttpContext>()))
                .Throws(new ArgumentException("Invalid", "orderId"));

            using var body = new MemoryStream();
            var context = new DefaultHttpContext();
            context.Response.Body = body;
            var errorHandlerMiddleware = new ErrorHandlerMiddleware(
                requestDelegate.Object,
                serviceCollection.BuildServiceProvider());

            // Act
            await errorHandlerMiddleware.Invoke(context);
            body.Position = 0;
            using var reader = new StreamReader(body);
            var stringModel = reader.ReadToEnd();
            var model = JsonSerializer.Deserialize<ErrorModel>(stringModel, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            Assert.AreEqual(400, context.Response.StatusCode);
            Assert.AreEqual("application/json", context.Response.ContentType);
            Assert.AreEqual(ErrorType.InvalidRequestError, model.Type);
            Assert.IsTrue(model.Params.Any(e => e.Name == "orderId"));
        }
    }
}
#pragma warning restore CA1506 // Avoid excessive class coupling