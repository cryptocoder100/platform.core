#pragma warning disable CA1054 // URI-like parameters should not be strings
#pragma warning disable CA2234 // Pass system uri objects instead of strings

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Exos.Platform.AspNetCore.Helpers;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Exos.Platform.AspNetCore.UnitTests.Helpers
{
    [TestClass]
    public class TelemetryHelperTests
    {
        [DataTestMethod]
        [DataRow("abc123", "abc123")]
        [DataRow("abc\r123", "abc\\r123")]
        public void TryEnrichRequestTelemetry_WithProperties_IncludesSanitizedProperties(string value, string expected)
        {
            // Arrange
            var context = new DefaultHttpContext();
            var telemetry = new RequestTelemetry();
            context.Features.Set(telemetry);

            // Act
            TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("key", value));

            // Assert
            var sp = (ISupportProperties)telemetry;
            expected.ShouldBe(sp.Properties["key"]);
        }

        [TestMethod]
        public void TryEnrichRequestTelemetry_WithoutRequestTelemetry_DoesNotFail()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act

            // Assert
            Should.NotThrow(() => TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("key", "value")));
        }

        [TestMethod]
        public void TryEnrichRequestTelemetry_WithNullHttpContext_ThrowsArgumentNullException()
        {
            // Arrange

            // Act

            // Assert
            Should.Throw<ArgumentNullException>(() => TelemetryHelper.TryEnrichRequestTelemetry(null, null));
        }

        [TestMethod]
        public void TryEnrichRequestTelemetry_WithNullProperties_ThrowsArgumentNullException()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act

            // Assert
            Should.Throw<ArgumentNullException>(() => TelemetryHelper.TryEnrichRequestTelemetry(context, null));
        }

        // [DataTestMethod]
        // [DataRow(null, null, null)]
        // [DataRow("", new string[0], "")]
        // public void RedactSqlQuerySpec_WithNoInput_IsSafe(string sqlQuerySpec, string[] keywords, string replacement)
        // {
        //     TelemetryHelper.RedactSqlQuerySpec(sqlQuerySpec, keywords, replacement);
        // }

        // [DataTestMethod]
        // [DataRow("{\"query\":\"\"}")]
        // [DataRow("{\"query\":\"\",\"parameters\":[]}")]
        // [DataRow("{\"parameters\":[{\"name\":\"@xyz\",\"value\":\"123\"}]}")]
        // public void RedactSqlQuerySpec_WithNoReplacements_WillReturnOriginalInstance(string sqlQuerySpec)
        // {
        //     var redactedSqlQuerySpec = TelemetryHelper.RedactSqlQuerySpec(sqlQuerySpec, new string[] { "abc" }, "REDACTED");
        //     Assert.AreSame(sqlQuerySpec, redactedSqlQuerySpec);
        // }

        // [DataTestMethod]
        // [DataRow("{\"parameters\":[{\"name\":\"@abc\",\"value\":\"123\"}]}", "{\"parameters\":[{\"name\":\"@abc\",\"value\":\"REDACTED\"}]}")]
        // [DataRow("{\"query\":\"sample\",\"parameters\":[{\"name\":\"@abc\",\"value\":\"123\"}]}", "{\"query\":\"sample\",\"parameters\":[{\"name\":\"@abc\",\"value\":\"REDACTED\"}]}")]
        // public void RedactSqlQuerySpec_WithKeywords_WillRedactKeywords(string sqlQuerySpec, string expectedSqlQueryString)
        // {
        //     var redactedSqlQuerySpec = TelemetryHelper.RedactSqlQuerySpec(sqlQuerySpec, new string[] { "abc" }, "REDACTED");
        //     Assert.AreEqual(expectedSqlQueryString, redactedSqlQuerySpec);
        // }

        [DataTestMethod]
        [DataRow(null, null, null)]
        [DataRow(null, null, "")]
        [DataRow(null, new string[0], "")]
        [DataRow(null, new string[] { "abc" }, "")]
        [DataRow("", new string[] { "abc" }, "")]
        [DataRow("other=123", new string[] { "abc" }, "")]
        [DataRow("other=123", new string[] { "abc" }, "")]
        [DataRow("other=123", new string[0], "")]
        [DataRow("other=123", new string[0], null)]
        [DataRow("other=123", null, null)]
        public void RedactQueryString_WithNoReplacements_WillReturnOriginalInstance(string queryString, string[] keywords, string replacement)
        {
            var redactedQueryString = TelemetryHelper.RedactQueryString(queryString, keywords, replacement);
            if (redactedQueryString.HasValue)
            {
                Assert.AreSame(queryString, redactedQueryString.Value);
            }
        }

        [DataTestMethod]
        [DataRow("other=123", new string[] { "abc" }, "other=123")]
        [DataRow("abc=123&other=123", new string[] { "abc" }, "abc=REDACTED&other=123")]
        [DataRow("abc=123&other=123&xyz=123", new string[] { "abc", "xyz" }, "abc=REDACTED&other=123&xyz=REDACTED")]
        public void RedactQueryString_WithKeywords_WillRedactKeywords(string queryString, string[] keywords, string expected)
        {
            var redactedQueryString = TelemetryHelper.RedactQueryString(queryString, keywords, "REDACTED");

            Assert.AreEqual(expected, redactedQueryString);
        }

        [DataTestMethod]
        [DataRow("?other=123", new string[] { "abc" }, "?other=123")]
        [DataRow("?abc=123&other=123", new string[] { "abc" }, "?abc=REDACTED&other=123")]
        [DataRow("?abc=123&other=123&xyz=123", new string[] { "abc", "xyz" }, "?abc=REDACTED&other=123&xyz=REDACTED")]
        public void RedactQueryString_WithLeadingQuestionMark_WillRetainLeadingQuestionMark(string queryString, string[] keywords, string expected)
        {
            var redactedQueryString = TelemetryHelper.RedactQueryString(queryString, keywords, "REDACTED");

            Assert.AreEqual(expected, redactedQueryString);
        }

        [DataTestMethod]
        [DataRow("other=123", new string[] { "abc" }, "HELLO", "other=123")]
        [DataRow("abc=123&other=123", new string[] { "abc" }, "HELLO", "abc=HELLO&other=123")]
        [DataRow("abc=123&other=123&xyz=123", new string[] { "abc", "xyz" }, "HELLO", "abc=HELLO&other=123&xyz=HELLO")]
        public void RedactQueryString_WithReplacement_WillUseReplacement(string queryString, string[] keywords, string replacement, string expected)
        {
            var redactedQueryString = TelemetryHelper.RedactQueryString(queryString, keywords, replacement);

            Assert.AreEqual(expected, redactedQueryString);
        }

        [DataTestMethod]
        [DataRow("other=", new string[] { "abc" }, "other=")]
        [DataRow("abc=&other=", new string[] { "abc" }, "abc=&other=")]
        [DataRow("abc=&other=&xyz", new string[] { "abc", "xyz" }, "abc=&other=&xyz")]
        public void RedactQueryString_WithIncompleteKeyValuePair_WillRetainIncompleteKeyValuePair(string queryString, string[] keywords, string expected)
        {
            var redactedQueryString = TelemetryHelper.RedactQueryString(queryString, keywords, "REDACTED");

            Assert.AreEqual(expected, redactedQueryString);
        }

        [DataTestMethod]
        [DataRow(null, null, null)]
        [DataRow("", null, null)]
        [DataRow(null, null, "")]
        [DataRow(null, new string[0], "")]
        [DataRow(null, new string[] { "abc" }, "")]
        [DataRow("", new string[] { "abc" }, "")]
        [DataRow("example.com", new string[] { "abc" }, "")]
        [DataRow("https://example.com/", new string[] { "abc" }, "")]
        public void RedactUrl_WithNoQuery_WillReturnOriginalInstance(string url, string[] keywords, string replacement)
        {
            var redactedUrl = TelemetryHelper.RedactUrl(url, keywords, replacement);

            Assert.AreSame(url, redactedUrl);
        }

        [DataTestMethod]
        [DataRow("https://example.com/path?other=123", new string[] { "abc" }, "https://example.com/path?other=123")]
        [DataRow("https://example.com/path?abc=123&other=123", new string[] { "abc" }, "https://example.com/path?abc=REDACTED&other=123")]
        [DataRow("https://example.com/path?abc=123&other=123&xyz=123", new string[] { "abc", "xyz" }, "https://example.com/path?abc=REDACTED&other=123&xyz=REDACTED")]
        [DataRow("path?abc=123&other=123&xyz=123", new string[] { "abc", "xyz" }, "path?abc=REDACTED&other=123&xyz=REDACTED")]
        public void RedactUrl_WithKeywords_WillRedactKeywords(string url, string[] keywords, string expected)
        {
            var redactedUrl = TelemetryHelper.RedactUrl(url, keywords, "REDACTED");

            Assert.AreEqual(expected, redactedUrl);
        }

        [DataTestMethod]
        [DataRow(null, null, null)]
        [DataRow("https://example.com/path", null, null)]
        [DataRow("https://example.com/path", null, "")]
        [DataRow("https://example.com/path", new string[0], "")]
        [DataRow("https://example.com/path", new string[] { "abc" }, "")]
        public void RedactUri_WithNoQuery_WillReturnOriginalInstance(string url, string[] keywords, string replacement)
        {
            Uri uri = null;
            if (url != null)
            {
                uri = new Uri(url);
            }

            var redactedUri = TelemetryHelper.RedactUrl(uri, keywords, replacement);

            Assert.AreSame(uri, redactedUri);
        }

        [DataTestMethod]
        [DataRow("https://example.com/path?other=123", new string[] { "abc" }, "https://example.com/path?other=123")]
        [DataRow("https://example.com/path?abc=123&other=123", new string[] { "abc" }, "https://example.com/path?abc=REDACTED&other=123")]
        [DataRow("https://example.com/path?abc=123&other=123&xyz=123", new string[] { "abc", "xyz" }, "https://example.com/path?abc=REDACTED&other=123&xyz=REDACTED")]
        public void RedactUri_WithKeywords_WillRedactKeywords(string url, string[] keywords, string expected)
        {
            var uri = new Uri(url);
            var redactedUri = TelemetryHelper.RedactUrl(uri, keywords, "REDACTED");

            var expectedUri = new Uri(expected);
            Assert.AreEqual(expectedUri, redactedUri);
        }

        [DataTestMethod]
        [DataRow("/path", new string[] { "abc" }, "")]
        [DataRow("/path?abc=123", new string[] { "abc" }, "")]
        public void RedactUri_WithRelativeUri_WillThrowInvalidOperationException(string url, string[] keywords, string replacement)
        {
            var uri = new Uri(url, UriKind.Relative);

            Assert.ThrowsException<InvalidOperationException>(() => TelemetryHelper.RedactUrl(uri, keywords, replacement));
        }
    }
}

#pragma warning restore CA1054 // URI-like parameters should not be strings
#pragma warning restore CA2234 // Pass system uri objects instead of strings