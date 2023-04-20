#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1040 // Avoid empty interfaces
#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA2201 // Do not raise reserved exception types
#pragma warning disable SA1201 // Elements should appear in the correct order

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exos.Platform.AspNetCore.UnitTests.DependencyInjection
{
    [TestClass]
    public class NamedServiceFactoryTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNamedSingleton_WithNullBuilder_ShouldThrowNullArgumentException()
        {
            var collection = new ServiceCollection();
            collection.AddNamedSingleton<IService>(null);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNamedSingleton_WithNullName_ShouldThrowNullArgumentException(string name)
        {
            var collection = new ServiceCollection();
            collection.AddNamedSingleton<IService>(builder =>
            {
                builder.Add<GoodService>(name);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddNamedSingleton_WithInvalidName_ShouldThrowInvalidOperationException()
        {
            var collection = new ServiceCollection();
            collection.AddNamedSingleton<IService>(builder =>
            {
            });

            var provider = collection.BuildServiceProvider();
            var factory = provider.GetRequiredService<INamedServiceFactory<IService>>();

            _ = factory.GetService("doesn't exist");
        }

        [TestMethod]
        public void AddNamedSingleton_WithImplementationType_ShouldReturnCachedImpelmentationType()
        {
            var collection = new ServiceCollection();
            collection.AddNamedSingleton<IService>(builder =>
            {
                for (int i = 0; i < 5; i++)
                {
                    builder.Add<GoodService>(i.ToString(CultureInfo.InvariantCulture));
                }
            });

            var provider = collection.BuildServiceProvider();
            var factory = provider.GetRequiredService<INamedServiceFactory<IService>>();

            for (int i = 0; i < 5; i++)
            {
                var ref1 = factory.GetService(i.ToString(CultureInfo.InvariantCulture));
                var ref2 = factory.GetService(i.ToString(CultureInfo.InvariantCulture));
                Assert.AreSame(ref1, ref2);
            }
        }

        [TestMethod]
        public void AddNamedSingleton_WithFactoryException_ShouldAlwaysReturnException()
        {
            var collection = new ServiceCollection();
            collection.AddNamedSingleton<IService>(builder =>
            {
                builder.Add("abc", sp => throw new Exception("blah"));
            });

            var provider = collection.BuildServiceProvider();
            var factory = provider.GetRequiredService<INamedServiceFactory<IService>>();

            Assert.ThrowsException<Exception>(() => factory.GetService("abc"));
            Assert.ThrowsException<Exception>(() => factory.GetService("abc"));
        }

        // Out of scope for singleton-only support
        /*
        [TestMethod]
        public void DisposeNamedServiceFactory_WithDisposableServices_ShouldDispose()
        {
            var collection = new ServiceCollection();
            var serviceInstance = new DisposableService();
            collection.AddNamedSingleton<IService>(builder =>
            {
                builder.Add("abc", sp => serviceInstance);
            });

            var provider = collection.BuildServiceProvider();
            provider.Dispose();

            Assert.IsTrue(serviceInstance.DisposeCalled);
        }
        */

        public interface IService
        {
        }

        public class GoodService : IService
        {
        }

        public class DisposableService : IService, IDisposable
        {
            public bool DisposeCalled { get; set; }

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }
    }
}
