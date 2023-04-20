#pragma warning disable CA1308 // Normalize strings to uppercase

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ExpectedObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exos.Platform.TenancyHelper.UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SerializationTests
    {
        private readonly System.Text.Json.JsonSerializerOptions _textOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        private readonly Newtonsoft.Json.JsonSerializerSettings _newtonSettings = new Newtonsoft.Json.JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
            }
        };

        [TestMethod]
        public void Serialization_WithSystemTextJson_ShouldMatchNewtonsoftJson()
        {
            // Arrange
            var model = new TestModel
            {
                Id = Guid.NewGuid().ToString().ToLowerInvariant(),
                Audit = new Models.AuditModel
                {
                    CreatedBy = "John",
                    CreatedDate = DateTimeOffset.Now,
                    IsDeleted = true,
                    LastUpdatedBy = null,
                    LastUpdatedDate = null
                },
                Tenant = new Models.TenantModel
                {
                    LineofBusinessid = new List<int> { 1, 2, 3 },
                    MasterClient = 123456,
                    ServicerGroup = -1,
                    ServicerIds = new List<long>(),
                    Vendor = 4567,
                    SubContractor = 789,
                    SubClient = 0
                },
                Version = "abc",
                _Etag = "123"
            };

            // Act
            var textJson = System.Text.Json.JsonSerializer.Serialize(model, _textOptions);
            var newtonJson = Newtonsoft.Json.JsonConvert.SerializeObject(model, _newtonSettings);

            // Assert
            Assert.AreEqual(textJson, newtonJson);
        }

        [TestMethod]
        public void Deserialization_WithSystemTextJson_ShouldMatchNewtonsoftJson()
        {
            // Arrange
            var model = new TestModel
            {
                Id = Guid.NewGuid().ToString().ToLowerInvariant(),
                Audit = new Models.AuditModel
                {
                    CreatedBy = "John",
                    CreatedDate = DateTimeOffset.Now,
                    IsDeleted = true,
                    LastUpdatedBy = null,
                    LastUpdatedDate = null
                },
                Tenant = new Models.TenantModel
                {
                    LineofBusinessid = new List<int> { 1, 2, 3 },
                    MasterClient = 123456,
                    ServicerGroup = -1,
                    ServicerIds = new List<long>(),
                    Vendor = 4567,
                    SubContractor = 789,
                    SubClient = 0
                },
                Version = "abc",
                _Etag = "123"
            };

            // Act
            var textJson = System.Text.Json.JsonSerializer.Serialize(model, _textOptions);
            var newtonJson = Newtonsoft.Json.JsonConvert.SerializeObject(model, _newtonSettings);
            var obj1 = System.Text.Json.JsonSerializer.Deserialize<TestModel>(textJson, _textOptions);
            var obj2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TestModel>(newtonJson, _newtonSettings);

            // Assert
            obj1.ToExpectedObject().ShouldEqual(obj2);
        }

        private class TestModel : Exos.Platform.TenancyHelper.Models.BaseModel
        {
            public override string CosmosDocType => "TestModel";
        }
    }
}
