using System;
using System.Threading.Tasks;
using Exos.Platform.DistributedCache.Redis.UnitTests.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Exos.Platform.DistributedCache.Redis.UnitTests
{
    public class GetSetRemoveTests
    {
        [Fact]
        public async Task GetMissingKey_ShouldReturnNull()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "non-existent-key";

            var result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAndGet_ShouldReturnObject()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var value = new byte[1];
            var key = "myKey";

            await cache.SetAsync(key, value);

            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task SetAndGet_ShouldWorkWithCaseSensitiveKeys()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var value = new byte[1];
            var key1 = "myKey";
            var key2 = "MyKey";

            await cache.SetAsync(key1, value);

            var result = await cache.GetAsync(key1);
            Assert.Equal(value, result);

            result = await cache.GetAsync(key2);
            Assert.Null(result);
        }

        [Fact]
        public async Task Set_ShouldAlwaysOverwrite()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var value1 = new byte[1] { 1 };
            var key = "myKey";

            await cache.SetAsync(key, value1);
            var result = await cache.GetAsync(key);
            Assert.Equal(value1, result);

            var value2 = new byte[1] { 2 };
            await cache.SetAsync(key, value2);
            result = await cache.GetAsync(key);
            Assert.Equal(value2, result);
        }

        [Fact]
        public async Task Remove_ShouldRemove()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var value = new byte[1] { 1 };
            var key = "myKey";

            await cache.SetAsync(key, value);
            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);

            await cache.RemoveAsync(key);
            result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task SetNull_ShouldThrowArgumentNull()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            byte[] value = null;
            var key = "myKey";

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return cache.SetAsync(key, value);
            });
        }
    }
}
