using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Exos.Platform.AspNetCore.UnitTests.Authentication
{
    [TestClass]
    public class TokenRevokerTest
    {
        private IDistributedCache _distributedCache;

        public TokenRevokerTest()
        {
            _distributedCache = new MemoryDistributedCache(
                Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
        }

        [TestMethod]
        public async Task IsTokenRevoked_shouldSucceedWithNoEmptyInput()
        {
            await TokenRevoker.IsTokenRevoked("TestToken", _distributedCache).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RevokeToken_shouldSucceedWithNoEmptyInput()
        {
            await TokenRevoker.RevokeToken("TestToken", _distributedCache).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IsTokenRevoked_shouldSReturnTrueIfPreviouslyRevoked()
        {
            var token = "TestToken";
            Assert.IsFalse(await TokenRevoker.IsTokenRevoked(token, _distributedCache).ConfigureAwait(false));
            await TokenRevoker.RevokeToken(token, _distributedCache).ConfigureAwait(false);
            Assert.IsTrue(await TokenRevoker.IsTokenRevoked(token, _distributedCache).ConfigureAwait(false));
        }
    }
}
