using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Exos.Platform.AspNetCore.Models;
using Exos.Platform.AspNetCore.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Exos.Platform.AspNetCore.UnitTests.Security
{
    [TestClass]
    public class UserClaimsReaderTests
    {
        public static IEnumerable<object[]> GetUserClaimsData()
        {
            yield return new object[] { File.ReadAllBytes("Data\\user-claims.01.json") };
            yield return new object[] { File.ReadAllBytes("Data\\user-claims.02.json") };
            // yield return new object[] { File.ReadAllBytes("Data\\user-claims.03.json") }; // Throws because claims can't have null data
            // yield return new object[] { File.ReadAllBytes("Data\\user-claims.04.json") }; // Throws because signature is null
        }

        [DataTestMethod]
        [DynamicData(nameof(GetUserClaimsData), DynamicDataSourceType.Method)]
        public void Read_MatchesExistingBehavior(byte[] utf8Json)
        {
            // Arrange
            using var sha = SHA256.Create();
            var claims = new List<Claim>();
            var cachedClaims = JsonSerializer.Deserialize<CachedClaimModel>(Encoding.UTF8.GetString(utf8Json));
            var compareClaims = new List<Claim>(cachedClaims.Claims.Select(c => new Claim(c.Type, c.Value)));
            var reader = new UserClaimsReader(utf8Json, claims, sha);

            // Act
            reader.Read();

            // Assert
            // NOTE: We can't validate the signature is correct in our unit tests because it is signed with an AKV
            // cert we don't have access to. What we are trying to accomplish here whether new and old deserialization
            // behaviors match.
            reader.Signature.ShouldBe(cachedClaims.Signature);
            reader.Claims.Select(c => KeyValuePair.Create(c.Type, c.Value)).ShouldBe(compareClaims.Select(c => KeyValuePair.Create(c.Type, c.Value)), ignoreOrder: true);
        }
    }
}
