namespace Exos.Platform.AspNetCore.UnitTests.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Converters;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ObjectJsonConverterTests
    {
        private readonly string _json = File.ReadAllTextAsync(@"Data\objectconverterjsonsample.json").Result;
        private JsonSerializerOptions _options;

        [TestInitialize]
        public void TestInit()
        {
            var sut = new ObjectJsonConverter();

            _options = new JsonSerializerOptions();
            _options.Converters.Add(sut);
        }

        [TestMethod]
        public void WhenRead_ThenSetAttributes()
        {
            var attributes = new TestEntityPolicyAttributes();
            var result = JsonSerializer.Deserialize<object>(_json, _options);

            if (result != null)
            {
                object policyDocJObject = result;
                dynamic policyDocDynamic = policyDocJObject;

                attributes.IsEntityMultiTenant = policyDocDynamic.isSubjectToMultiTenancy != null && policyDocDynamic.isSubjectToMultiTenancy == true;
                attributes.IsCacheable = policyDocDynamic.isCacheable != null && policyDocDynamic.isCacheable == true;
                attributes.ApplyServicerFilterForVendorTenant = policyDocDynamic.GetType().GetProperty("applyServicerFilterForVendorTenant") != null ? policyDocDynamic.applyServicerFilterForVendorTenant : true;
            }

            Assert.AreEqual(attributes.IsEntityMultiTenant, false);
            Assert.AreEqual(attributes.IsCacheable, true);
            Assert.AreEqual(attributes.ApplyServicerFilterForVendorTenant, true);
        }

        [TestMethod]
        public void WhenWrite_ThenWriteJsonAttributes()
        {
            var entity = new TestEntityPolicyAttributes
            {
                IsEntityMultiTenant = false,
                IsCacheable = true,
                ApplyServicerFilterForVendorTenant = true
            };

            var result = JsonSerializer.Serialize<object>(entity, _options);

            Assert.AreEqual(result, _json);
        }

        private class TestEntityPolicyAttributes
        {
            [JsonPropertyName("isSubjectToMultiTenancy")]
            public bool IsEntityMultiTenant { get; set; }

            [JsonPropertyName("isCacheable")]
            public bool IsCacheable { get; set; }

            [JsonPropertyName("applyServicerFilterForVendorTenant")]
            public bool ApplyServicerFilterForVendorTenant { get; set; }
        }
    }
}
