using System;
using System.Threading;
using System.Threading.Tasks;
using Exos.Platform.DistributedCache.Redis.UnitTests.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Exos.Platform.DistributedCache.Redis.UnitTests
{
    public class ExpirationTests
    {
        [Fact]
        public async Task AbsoluteExpirationInThePast_ShouldThrowArgumentOutOfRange()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var expected = DateTimeOffset.Now - TimeSpan.FromMinutes(1);
            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            {
                return cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(expected));
            });
        }

        [Fact]
        public async Task AbsoluteExpiration_ShouldExpire()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 4 && (result != null); i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                result = await cache.GetAsync(key);
            }

            Assert.Null(result);
        }

        [Fact]
        public async Task AbsoluteSubSecondExpiration_ShouldExpireImmediately()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

            var result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task NegativeRelativeExpiration_ShouldThrowArgumentOutOfRange()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            {
                return cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(-1)));
            });
        }

        [Fact]
        public async Task ZeroRelativeExpiration_ShouldThrowArgumentOutOfRange()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            {
                return cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero));
            });
        }

        [Fact]
        public async Task RelativeExpiration_ShouldExpire()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 4 && (result != null); i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                result = await cache.GetAsync(key);
            }

            Assert.Null(result);
        }

        [Fact]
        public async Task RelativeSubSecondExpiration_ShouldExpireImmediately()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

            var result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task NegativeSlidingExpiration_ShouldThrowArgumentOutOfRange()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            {
                return cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(-1)));
            });
        }

        [Fact]
        public async Task ZeroSlidingExpiration_ShouldThrowArgumentOutOfRange()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            {
                return cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.Zero));
            });
        }

        [Fact]
        public async Task SlidingExpiration_ShouldExpireIfNotAccessed()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task SlidingSubSecondExpiration_ShouldExpireImmediately()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(0.25)));

            var result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task SlidingExpiration_ShouldBeRenewedByAccess()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(3)));

            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                result = await cache.GetAsync(key);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(3));
            result = await cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task SlidingExpiration_ShouldBeRenewedByAccessUntilAbsoluteExpiration()
        {
            var cache = RedisDistributedCacheHelper.CreateInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(3))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(8)));

            var result = await cache.GetAsync(key);
            Assert.Equal(value, result);

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                result = await cache.GetAsync(key);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(8));

            result = await cache.GetAsync(key);
            Assert.Null(result);
        }
    }
}
