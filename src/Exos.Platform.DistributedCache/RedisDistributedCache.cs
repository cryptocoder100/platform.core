#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SX1309 // Field names should begin with underscore
using System;
using System.Threading;
using System.Threading.Tasks;
using Exos.Platform.RedisConnectionPool;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Exos.Platform.DistributedCache.Redis
{
    /// <summary>
    /// An implementation of the <see cref="IDistributedCache" /> interface using Redis.
    /// </summary>
    public class RedisDistributedCache : IDistributedCache
    {
        private const string ABSOLUTE_EXPIRATION_KEY = "absolute-expiration";
        private const string SLIDING_EXPIRATION_KEY = "sliding-expiration";
        private const string DATA_KEY = "data";
        private const string CONNECTION_KEY = "Connection";
        private const long INVALID = -1;

        // The intent here is set the hash and expiry in the same call
        private readonly string SET_SCRIPT = $@"
            redis.call('HMSET', KEYS[1], '{ABSOLUTE_EXPIRATION_KEY}', ARGV[1], '{SLIDING_EXPIRATION_KEY}', ARGV[2], '{DATA_KEY}', ARGV[3])
            if ARGV[4] ~= '{INVALID}' then
                redis.call('EXPIRE', KEYS[1], ARGV[4])
            end
            return 1";

        private readonly AsyncRetryPolicy _policy;
        private readonly RedisDistributedCacheOptions _options;
        private readonly IConnectionMultiplexerPool _pool;
        private readonly TelemetryClient _telemetryClient;
        private readonly string _instanceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDistributedCache" /> class.
        /// </summary>
        /// <param name="optionsAccessor">An instance of <see cref="RedisDistributedCacheOptions" />.</param>
        /// <param name="pool">An instance of <see cref="IConnectionMultiplexerPool" />.</param>
        /// <param name="telemetryClient">Optional. A <see cref="TelemetryClient" /> for tracking dependency calls and exceptions.</param>
        public RedisDistributedCache(IOptions<RedisDistributedCacheOptions> optionsAccessor, IConnectionMultiplexerPool pool, TelemetryClient telemetryClient = null)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (optionsAccessor == pool)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            _options = optionsAccessor.Value;
            _instanceName = _options.InstanceName ?? string.Empty;
            _pool = pool;
            _telemetryClient = telemetryClient;

            _policy = Policy
                .Handle<Exception>(ex => !(ex is TaskCanceledException))
                .WaitAndRetryAsync(
                    retryCount: _options.MaxRetries,
                    sleepDurationProvider: attempt =>
                    {
                        var seconds = 1.0 * Math.Pow(2, attempt - 1); // Exponential back-off; 1s, 2s, 4s, 8s, 16s, 32s, etc...
                        return TimeSpan.FromSeconds(Math.Min(seconds, 30)); // Clamp wait to 30s max
                    },
                    onRetry: (exception, sleepDuration, context) =>
                    {
                        // Report the connection as failed; we get a new one on the next loop
                        if (context.TryGetValue(CONNECTION_KEY, out var connection) && connection != null)
                        {
                            _pool.FailConnection(connection as IConnectionMultiplexer, exception);
                        }
                        else if (_telemetryClient != null)
                        {
                            var telemetry = new ExceptionTelemetry(exception)
                            {
                                SeverityLevel = SeverityLevel.Critical,
                                Message = $"A retry policy failed, but did not contain a ({nameof(IConnectionMultiplexer)}) connection. {exception?.Message}",
                            };
                            telemetry.Properties["SleepDuration"] = sleepDuration.ToString();
                            telemetry.Properties["OperationKey"] = context.OperationKey;
                            _telemetryClient.TrackException(telemetry);
                        }
                    });
        }

        /// <summary>
        /// Gets a value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the located value or null.</returns>
        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var foundFailFastKey = _options.FailFastKeys.Find(s => key.StartsWith(s, StringComparison.OrdinalIgnoreCase));

            var instanceKey = _instanceName + key;

            token.ThrowIfCancellationRequested();

            RedisValue[] values;
            if (!string.IsNullOrEmpty(foundFailFastKey))
            {
                // Fail fast key no retry policy.(UserClaimsCacheKey,UserClaimsWorkOrdersCacheKey)
                values = await GetRedisValuesAsync(instanceKey, token);
            }
            else
            {
                values = await _policy.ExecuteAsync<RedisValue[]>(
                   async (context, ct) =>
                   {
                       // Get a connection and place it in context so any error handling can report it as failed
                       var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
                       var database = connection.GetDatabase();
                       context[CONNECTION_KEY] = connection;

                       // Check cancellation
                       ct.ThrowIfCancellationRequested();

                       // Get me some data
                       return await database.HashGetAsync(
                              instanceKey,
                              new RedisValue[] { ABSOLUTE_EXPIRATION_KEY, SLIDING_EXPIRATION_KEY, DATA_KEY },
                              telemetryClient: _telemetryClient).ConfigureAwait(false);
                   },
                   new Context($"GetAsync({nameof(key)}, {nameof(token)}) - HashGetAsync"),
                   token).ConfigureAwait(false);
            }

            if (values.Length < 3 || values[2] == RedisValue.Null)
            {
                // Cache miss
                return null;
            }

            DateTimeOffset? absoluteExpiration = values[0] == INVALID ? (DateTimeOffset?)null : new DateTimeOffset((long)values[0], TimeSpan.Zero);
            TimeSpan? slidingExpiration = values[1] == INVALID ? (TimeSpan?)null : new TimeSpan((long)values[1]);
            byte[] data = values[2];

            if (slidingExpiration != null)
            {
                // Calculate TTL
                var ttl = CalculateTimeToLive(DateTimeOffset.UtcNow, absoluteExpiration, slidingExpiration);
                var expiry = ttl == null ? (TimeSpan?)null : TimeSpan.FromSeconds((double)ttl);

                // Debug.WriteLine("GetAsync Now: " + DateTimeOffset.UtcNow.ToLocalTime().ToString());
                // Debug.WriteLine("GetAsync Absolute: " + absoluteExpiration.Value.ToLocalTime().ToString());
                // Debug.WriteLine("GetAsync Sliding: " + slidingExpiration.Value.TotalSeconds);
                // Debug.WriteLine("GetAsync TTL: " + ttl);
                token.ThrowIfCancellationRequested();
                await _policy.ExecuteAsync(
                    async (context, ct) =>
                    {
                        // Get a connection and place it in context so any error handling can report it as failed
                        var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
                        var database = connection.GetDatabase();
                        context[CONNECTION_KEY] = connection;

                        // Check cancellation
                        ct.ThrowIfCancellationRequested();

                        // Update expiry
                        await database.KeyExpireAsync(
                            instanceKey,
                            expiry,
                            telemetryClient: _telemetryClient).ConfigureAwait(false);
                    },
                    new Context($"GetAsync({nameof(key)}, {nameof(token)}) - KeyExpireAsync"),
                    token).ConfigureAwait(false);
            }

            return data;
        }

        /// <summary>
        /// Refreshes a value in the cache based on its key, resetting its sliding expiration timeout (if any).
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var instanceKey = _instanceName + key;

            token.ThrowIfCancellationRequested();
            var values = await _policy.ExecuteAsync<RedisValue[]>(
                async (context, ct) =>
                {
                    // Get a connection and place it in context so any error handling can report it as failed
                    var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
                    var database = connection.GetDatabase();
                    context[CONNECTION_KEY] = connection;

                    // Check cancellation
                    ct.ThrowIfCancellationRequested();

                    // Get me some data
                    return await database.HashGetAsync(
                        instanceKey,
                        new RedisValue[] { ABSOLUTE_EXPIRATION_KEY, SLIDING_EXPIRATION_KEY },
                        telemetryClient: _telemetryClient).ConfigureAwait(false);
                },
                new Context($"RefreshAsync({nameof(key)}, {nameof(token)}) - HashGetAsync"),
                token).ConfigureAwait(false);

            if (values.Length < 2 || values[0] == RedisValue.Null)
            {
                // Cache mis
                return;
            }

            // Map values
            DateTimeOffset? absoluteExpiration = values[0] == INVALID ? (DateTimeOffset?)null : new DateTimeOffset((long)values[0], TimeSpan.Zero);
            TimeSpan? slidingExpiration = values[1] == INVALID ? (TimeSpan?)null : new TimeSpan((long)values[1]);

            if (slidingExpiration != null)
            {
                // Calculate TTL
                var ttl = CalculateTimeToLive(DateTimeOffset.UtcNow, absoluteExpiration, slidingExpiration);
                var expiry = ttl == null ? (TimeSpan?)null : TimeSpan.FromSeconds((double)ttl);

                token.ThrowIfCancellationRequested();
                await _policy.ExecuteAsync(
                    async (context, ct) =>
                    {
                        // Get a connection and place it in context so any error handling can report it as failed
                        var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
                        var database = connection.GetDatabase();
                        context[CONNECTION_KEY] = connection;

                        // Check cancellation
                        ct.ThrowIfCancellationRequested();

                        // Update expiry
                        await database.KeyExpireAsync(
                            instanceKey,
                            expiry,
                            telemetryClient: _telemetryClient).ConfigureAwait(false);
                    },
                    new Context($"RefreshAsync({nameof(key)}, {nameof(token)}) - KeyExpireAsync"),
                    token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            var instanceKey = _instanceName + key;

            await _policy.ExecuteAsync(
                async (context, ct) =>
                {
                    // Get a connection and place it in context so any error handling can report it as failed
                    var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
                    var database = connection.GetDatabase();
                    context[CONNECTION_KEY] = connection;

                    // Check cancellation
                    ct.ThrowIfCancellationRequested();

                    // Delete the key
                    await database.KeyDeleteAsync(
                        instanceKey,
                        telemetryClient: _telemetryClient).ConfigureAwait(false);
                },
                new Context($"RemoveAsync({nameof(key)}, {nameof(token)}) - KeyDeleteAsync"),
                token).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the value with the given key.
        /// </summary>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="value">The value to set in the cache.</param>
        /// <param name="options">The cache options for the value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task" /> that represents the asynchronous operation.</returns>
        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var instanceKey = _instanceName + key;
            var creationTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = CalculateAbsoluteExpiration(creationTime, options.AbsoluteExpiration, options.AbsoluteExpirationRelativeToNow);

            // Debug.WriteLine("SetAsync Now: " + creationTime.ToLocalTime().ToString());
            // Debug.WriteLine("SetAsync Absolute: " + absoluteExpiration.Value.ToLocalTime().ToString());
            token.ThrowIfCancellationRequested();
            await _policy.ExecuteAsync(
                async (context, ct) =>
                {
                    // Get a connection and place it in context so any error handling can report it as failed
                    var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
                    var database = connection.GetDatabase();
                    context[CONNECTION_KEY] = connection;

                    // Check cancellation
                    ct.ThrowIfCancellationRequested();

                    // Set the key
                    await database.ScriptEvaluateAsync(
                        SET_SCRIPT,
                        new RedisKey[] { instanceKey },
                        new RedisValue[] { absoluteExpiration?.Ticks ?? INVALID, options.SlidingExpiration?.Ticks ?? INVALID, value, CalculateTimeToLive(creationTime, absoluteExpiration, options.SlidingExpiration) ?? INVALID },
                        telemetryClient: _telemetryClient).ConfigureAwait(false);
                },
                new Context($"SetAsync({nameof(key)}, {nameof(value)}, {nameof(options)}, {nameof(token)}) - ScriptEvaluateAsync"),
                token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        byte[] IDistributedCache.Get(string key)
        {
            throw new NotSupportedException("Synchronous operations are not supported.");
        }

        /// <inheritdoc />
        void IDistributedCache.Refresh(string key)
        {
            throw new NotSupportedException("Synchronous operations are not supported.");
        }

        /// <inheritdoc />
        void IDistributedCache.Remove(string key)
        {
            throw new NotSupportedException("Synchronous operations are not supported.");
        }

        /// <inheritdoc />
        void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw new NotSupportedException("Synchronous operations are not supported.");
        }

        // https://github.com/aspnet/Extensions/blob/release/2.2/src/Caching/StackExchangeRedis/src/RedisCache.cs#L394-L411
        private static long? CalculateTimeToLive(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration, TimeSpan? slidingExpiration)
        {
            if (absoluteExpiration.HasValue && slidingExpiration.HasValue)
            {
                return (long)Math.Min(
                    (absoluteExpiration.Value - creationTime).TotalSeconds,
                    slidingExpiration.Value.TotalSeconds);
            }
            else if (absoluteExpiration.HasValue)
            {
                return (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
            }
            else if (slidingExpiration.HasValue)
            {
                return (long)slidingExpiration.Value.TotalSeconds;
            }

            return null;
        }

        // https://github.com/aspnet/Extensions/blob/release/2.2/src/Caching/StackExchangeRedis/src/RedisCache.cs#L413-L429
        private static DateTimeOffset? CalculateAbsoluteExpiration(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration, TimeSpan? absoluteExpirationRelativeToNow)
        {
            if (absoluteExpiration.HasValue && absoluteExpiration <= creationTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(absoluteExpiration),
                    absoluteExpiration.Value,
                    "The absolute expiration value must be in the future.");
            }

            var result = absoluteExpiration;
            if (absoluteExpirationRelativeToNow.HasValue)
            {
                result = creationTime + absoluteExpirationRelativeToNow;
            }

            return result;
        }

        private async Task<RedisValue[]> GetRedisValuesAsync(string instanceKey, CancellationToken token)
        {
            // Get a connection and place it in context so any error handling can report it as failed
            var connection = await _pool.GetConnectionAsync().ConfigureAwait(false);
            var database = connection.GetDatabase();

            // Check cancellation
            token.ThrowIfCancellationRequested();

            try
            {
                // Get me some data
                var values = await database.HashGetAsync(
                            instanceKey,
                            new RedisValue[] { ABSOLUTE_EXPIRATION_KEY, SLIDING_EXPIRATION_KEY, DATA_KEY },
                            telemetryClient: _telemetryClient);
                return values;
            }
            catch (Exception ex)
            {
                _pool.FailConnection(connection as IConnectionMultiplexer, ex);
                throw;
            }
        }
    }
}
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning restore SA1306 // Field names should begin with lower-case letter
#pragma warning restore SX1309 // Field names should begin with underscore