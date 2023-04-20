namespace Exos.Platform.AspNetCore.UnitTests.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Text.RegularExpressions;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Extensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StringExtensionsTest
    {
        [TestMethod]
        public void CamelCaseStringGivenPascalCaseString()
        {
            var test = "CamelCase";
            var result = test.ToCamelCaseInvariant();

            Assert.AreEqual(result, "camelCase");
        }

        [TestMethod]
        public void EncryptionDecryption()
        {
            string workOrderId = "1234556666222";
            string pvtKey = "ShVkYp3s6v9y$B&E";
            // string pvtKey = "r4u7x!A%D*G-KaPdSgUkXp2s5v8y/B?E";
            string stringToEncrypt = workOrderId + "|" + DateTime.UtcNow.Ticks + "|" + DateTime.UtcNow;
            var encryptedStr = AesEncryption.Encrypt(stringToEncrypt, pvtKey);
            var decryptedStr = AesEncryption.Decrypt(encryptedStr, pvtKey);
            Assert.IsFalse(encryptedStr == stringToEncrypt);
        }

        [TestMethod]
        public void TestMaskingString()
        {
            string telemetryValue =
                "https://signalrsvc.dev.exostechnology.internal/api/v1/NotificationsHub/connect/negotiate?exossignalraffinityid=4c96c9b8-0842-4bae-8f22-74c72cfbe8be&oauth_token=eyJ* " +
                "https://documentsvc.dev.exostechnology.internal/api/v1/document/2e0f6604-f121-496f-a131-0cf7922d2f96/raw?oauth_token=eyJ*&headercsrftoken=null " +
                "An IConnectionMultiplexer in the IConnectionMultiplexerPool failed and has to be replaced. No connection is active/available to service this operation: " +
                "HMGET UserClaimsCacheKey:'eyJ4656564564564564564564564564564'; Operation canceled ValidateAntiforgeryTokenMiddleware is validating " +
                "for '/documentsvc/v1/document/629f78f2-ea30-427b-ab44-be24d5851b8c/raw?headercsrftoken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0b2tlb " +
                "\"ssn\\\\\\\":\\\\\\\"298568837\\\\\\\",\\\\\\\"type\\\\\\\":\\\\\\\"CoBorrower\\\\\\\",";

            string maskValues = "password|ssn|oauth_token|headercsrftoken|UserClaimsCacheKey";
            string regularExpression = $"w*({maskValues})([^\\s]+)";

            var matches = Regex.Matches(telemetryValue, regularExpression, RegexOptions.IgnoreCase);

            string maskedValue = Regex.Replace(
                telemetryValue,
                regularExpression,
                m =>
                {
                    // Mask the value
                    var g = m.Groups[2];
                    var sb = new StringBuilder(m.Value);
                    // sb.Replace(g.Value, new string('*', g.Length), g.Index - m.Index, g.Length);
                    sb.Replace(g.Value, new string('*', 4), g.Index - m.Index, g.Length);
                    return sb.ToString();
                },
                RegexOptions.IgnoreCase);

            Assert.IsFalse(telemetryValue.Equals(maskedValue, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"Masked Value={maskedValue}");
        }
    }
}
