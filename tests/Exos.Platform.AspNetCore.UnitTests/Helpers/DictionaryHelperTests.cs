using Exos.Platform.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exos.Platform.AspNetCore.UnitTests.Extensions;

[TestClass]
public class DictionaryHelperTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void GetValueOrDefault_WithNullDictionary_ThrowsNullArgumentException()
    {
        DictionaryHelper.GetValueOrDefault<string, object>(null, "abc");
    }

    [TestMethod]
    public void GetValueOrDefault_WithKey_ReturnsValueOrDefault()
    {
        var obj = "123";
        IDictionary<string, object> dictionary = new Dictionary<string, object>
        {
            { "abc", obj }
        };

        Assert.AreSame(obj, DictionaryHelper.GetValueOrDefault<string, object>(dictionary, "abc"));
        Assert.AreEqual(default(string), DictionaryHelper.GetValueOrDefault<string, object>(dictionary, "xyz"));
    }
}
