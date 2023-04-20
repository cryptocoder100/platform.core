namespace Exos.Platform.Persistence.Tests.Encryption
{
    using Exos.Platform.Persistence.Encryption;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EncryptedFieldHeaderParserTests
    {
        [TestMethod]
        public void HappyPath()
        {
            var test = new EncryptedFieldHeaderParser("happy|ff944b2f-4d3c-4c55-8a18-81fd1d5453bb|test");

            Assert.AreEqual("happy", test.EncryptionKey.KeyName);
            Assert.AreEqual("ff944b2f-4d3c-4c55-8a18-81fd1d5453bb", test.EncryptionKey.KeyVersion);
            Assert.AreEqual("test", test.Cypher);

            Assert.IsTrue(test.EncryptionKey.IsHeaderComplete);
            Assert.IsTrue(test.IsEncrypted);
        }

        [TestMethod]
        public void TestingSizeLimits()
        {
            var test = new EncryptedFieldHeaderParser("happyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappy1234567|ff944b2f-4d3c-4c55-8a18-81fd1d5453bb|test");

            Assert.AreEqual("happyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappy1234567", test.EncryptionKey.KeyName);
            Assert.AreEqual("ff944b2f-4d3c-4c55-8a18-81fd1d5453bb", test.EncryptionKey.KeyVersion);
            Assert.AreEqual("test", test.Cypher);

            Assert.IsTrue(test.EncryptionKey.IsHeaderComplete);
            Assert.IsTrue(test.IsEncrypted);

            test = new EncryptedFieldHeaderParser("h|ff944b2f-4d3c-4c55-8a18-81fd1d5453bb|test");

            Assert.AreEqual("h", test.EncryptionKey.KeyName);
            Assert.AreEqual("ff944b2f-4d3c-4c55-8a18-81fd1d5453bb", test.EncryptionKey.KeyVersion);
            Assert.AreEqual("test", test.Cypher);

            Assert.IsTrue(test.EncryptionKey.IsHeaderComplete);
            Assert.IsTrue(test.IsEncrypted);

            test = new EncryptedFieldHeaderParser("happyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappyhappy12345678|ff944b2f-4d3c-4c55-8a18-81fd1d5453bb|test");

            Assert.IsNull(test.EncryptionKey);
            Assert.IsFalse(test.IsEncrypted);
        }

        [TestMethod]
        public void HappyPathNotEncrypted()
        {
            var test = new EncryptedFieldHeaderParser("Notencrypted");

            Assert.IsFalse(test.IsEncrypted);
            Assert.IsNull(test.EncryptionKey);
        }

        [TestMethod]
        public void EmbeddedJson()
        {
            var testString = @"{
                ""trackingId"": ""965acc23-39ab-434e-aa3f-79ede814c933"",
                ""technology"": ""EXOSTITLE"",
                ""clientId"": ""123456"",
                ""referenceId"": ""key--exos-svclnk|e2468dc3e8c549b2909ade7e66ceff34|cypher"",
                ""eventTypeId"": 0,
                ""eventName"": ""TDERJT"",
                ""eventId"": """",
                ""eventDate"": ""2020-12-14T07:09:59.658526+00:00"",
                ""eventData"": {
                    ""response"": {
                        ""errors"": [
                        {
                            ""description"": ""TDE Initiate ProcessFailed""
                        }
                        ],
                        ""processStatuses"": []
                    },
                    ""transactionHeader"": {
                        ""date"": ""2020-12-14T07:09:59.6289837+00:00"",
                        ""id"": ""bla"",
                        ""isRegeneration"": false,
                        ""receiver"": ""Exos"",
                        ""referenceId"": ""bla"",
                        ""sender"": ""EXOSTITLE"",
                        ""systemId"": ""8""
                    }
                }
            }";

            var test = new EncryptedFieldHeaderParser(testString);

            Assert.IsFalse(test.IsEncrypted);
            Assert.IsNull(test.EncryptionKey);
        }
    }
}