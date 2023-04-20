namespace Exos.Platform.AspNetCore.UnitTests.Helpers
{
    using System.Diagnostics.CodeAnalysis;
    using Exos.Platform.AspNetCore.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Shouldly;

    [TestClass]
    public class LoggerHelperTests
    {
        [DataTestMethod]
        [DataRow(null, null)]
        [DataRow("", "")]
        [DataRow("abc123", "abc123")]
        [DataRow("\"", "\\\"")]
        [DataRow("\\", "\\\\")]
        [DataRow("/", "/")]
        [DataRow("\b", "\\b")]
        [DataRow("\f", "\\f")]
        [DataRow("\n", "\\n")]
        [DataRow("\r", "\\r")]
        [DataRow("\t", "\\t")]
        [DataRow(0, "0")]
        [DataRow(1, "1")]
        [DataRow(true, "true")]
        [DataRow(false, "false")]
        [DataRow(false, "false")]
        [DataRow(new[] { "key", "value" }, "[\"key\",\"value\"]")]
        [DataRow(new[] { "key", "\r" }, "[\"key\",\"\\r\"]")]
        public void SanitizeValue_WithTestData_ReturnsSanitizedValue(object obj, string expected)
        {
            // Arrange

            // Act
            var sanitizedStr = LoggerHelper.SanitizeValue(obj);

            // Assert
            expected?.ShouldBe(sanitizedStr);
        }
    }
}
