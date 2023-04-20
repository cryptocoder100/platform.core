#pragma warning disable CS0618

namespace Exos.Platform.AspNetCore.UnitTests.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GuidExtensionsTest
    {
        [TestMethod]
        public void GuidToShortIdIsAlphaNumericOnly()
        {
            var guid = Guid.NewGuid();
            var shortId = guid.ToShortId();
            var regex = new Regex("^[a-zA-Z0-9]+$");

            Assert.IsTrue(regex.IsMatch(shortId));
        }

        [TestMethod]
        public void GuidToShortIdIsShorterThanGuidString()
        {
            var guid = Guid.NewGuid();
            var originalId = guid.ToString();
            var shortId = guid.ToShortId();

            Assert.IsTrue(shortId.Length < originalId.Length);
        }
    }
}
#pragma warning restore CS0618