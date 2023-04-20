namespace Exos.Platform.AspNetCore.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Exos.Platform.AspNetCore.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AssemblyHelperTests
    {
        [TestMethod]
        public void AllMethods_WithNullAssembly_ThrowArgumentNullException()
        {
            // Assert
            Assert.ThrowsException<ArgumentNullException>(() => AssemblyHelper.GetName(null));
            Assert.ThrowsException<ArgumentNullException>(() => AssemblyHelper.GetVersion(null));
            // Assert.ThrowsException<ArgumentNullException>(() => AssemblyHelper.GetCopyright(null));
        }
    }
}
