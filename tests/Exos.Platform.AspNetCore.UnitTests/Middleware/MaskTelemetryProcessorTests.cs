#pragma warning disable CA1054 // URI-like parameters should not be strings

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Exos.Platform.AspNetCore.Middleware;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Exos.Platform.AspNetCore.UnitTests.Middleware
{
    [TestClass]
    public class MaskTelemetryProcessorTests
    {
        [DataTestMethod]
        [DataRow("https://example.com/path?abc=123", null)]
        [DataRow("https://example.com/path?abc=123", "https://ui.example.com/path?abc=123")]
        public void Process_WithRequestTelemetry_WillRedactKeywords(string url, string referrer)
        {
            var next = new Mock<ITelemetryProcessor>();
            next.Setup(n => n.Process(It.IsAny<ITelemetry>()));

            var options = Options.Create(new MaskTelemetryProcessorOptions
            {
                MaskTelemetryValues = new List<string>
                {
                    "abc"
                }
            });

            var processor = new MaskTelemetryProcessor(next.Object, options);
            var requestTelemetry = new RequestTelemetry
            {
                Url = new Uri(url),
            };

            if (referrer != null)
            {
                requestTelemetry.Properties["Referrer"] = referrer;
            }

            processor.Process(requestTelemetry);

            Assert.IsTrue(requestTelemetry.Url.ToString().Contains("REDACTED", StringComparison.Ordinal));
            if (referrer != null)
            {
                Assert.IsTrue(requestTelemetry.Properties["Referrer"].Contains("REDACTED", StringComparison.Ordinal));
            }
        }

        [Ignore("Cosmos SQL redacting has been disabled")]
        [DataTestMethod]
        [DataRow("{\"query\":\"sample\",\"parameters\":[{\"name\":\"@abc\",\"value\":\"123\"}]}")]
        public void Process_WithCosmosDependencyTelemetry_WillRedactKeywords(string sqlQuerySpec)
        {
            var next = new Mock<ITelemetryProcessor>();
            next.Setup(n => n.Process(It.IsAny<ITelemetry>()));

            var options = Options.Create(new MaskTelemetryProcessorOptions
            {
                MaskTelemetryValues = new List<string>
                {
                    "abc"
                }
            });

            var processor = new MaskTelemetryProcessor(next.Object, options);
            var requestTelemetry = new DependencyTelemetry
            {
                Type = "COSMOSDB"
            };

            requestTelemetry.Properties["sqlQuerySpec"] = sqlQuerySpec;
            processor.Process(requestTelemetry);

            var redactedSqlQuerySpect = requestTelemetry.Properties["sqlQuerySpec"];
            Assert.IsTrue(redactedSqlQuerySpect.Contains("REDACTED", StringComparison.Ordinal));
        }
    }
}
