using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using StackExchange.Redis;

namespace Exos.Platform.DistributedCache.Redis
{
    /// <summary>
    /// Helper methods to access Redis.
    /// </summary>
    internal static class DatabaseExtensions
    {
        /// <summary>
        /// Set a timeout on key.
        /// </summary>
        /// <param name="database">Redis instance.</param>
        /// <param name="key">The key to set the expiration for.</param>
        /// <param name="expiry">The timeout to set.</param>
        /// <param name="flags">The flags to use.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        /// <returns>True if the timeout was set. false if key does not exist or the timeout could not be set.</returns>
        public static async Task<bool> KeyExpireAsync(this IDatabase database, RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, TelemetryClient telemetryClient = null)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            RedisException redisException = null;
            RedisTimeoutException timeoutException = null;

            try
            {
                bool result = await database.KeyExpireAsync(key, expiry).ConfigureAwait(false);
                return result;
            }
            catch (RedisException ex)
            {
                redisException = ex;
                throw;
            }
            catch (RedisTimeoutException ex)
            {
                timeoutException = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                TrackDependency(database, telemetryClient, startTime, stopwatch.Elapsed, "Key Expire (EXPIRE)", ResultType.None, redisException, timeoutException);
            }
        }

        /// <summary>
        /// Returns the values associated with the specified fields in the hash stored at key.
        /// </summary>
        /// <param name="database">Redis instance.</param>
        /// <param name="key">The key of the hash.</param>
        /// <param name="hashFields">The fields in the hash to delete.</param>
        /// <param name="flags">The flags to use.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <returns>List of values associated with the given fields, in the same order as they are requested.</returns>
        public static async Task<RedisValue[]> HashGetAsync(this IDatabase database, RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None, TelemetryClient telemetryClient = null)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            RedisException redisException = null;
            RedisTimeoutException timeoutException = null;

            try
            {
                RedisValue[] result = await database.HashGetAsync(key, hashFields).ConfigureAwait(false);
                return result;
            }
            catch (RedisException ex)
            {
                redisException = ex;
                throw;
            }
            catch (RedisTimeoutException ex)
            {
                timeoutException = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                TrackDependency(database, telemetryClient, startTime, stopwatch.Elapsed, "Hash Get (HMGET)", ResultType.MultiBulk, redisException, timeoutException);
            }
        }

        /// <summary>
        ///  Execute a Lua script against the server.
        /// </summary>
        /// <param name="database">Redis instance.</param>
        /// <param name="script">The script to execute.</param>
        /// <param name="keys">The keys to execute against.</param>
        /// <param name="values">The values to execute against.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <returns>A dynamic representation of the script's result.</returns>
        public static async Task<RedisResult> ScriptEvaluateAsync(this IDatabase database, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, TelemetryClient telemetryClient = null)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            RedisResult result = null;
            RedisException redisException = null;
            RedisTimeoutException timeoutException = null;

            try
            {
                result = await database.ScriptEvaluateAsync(script, keys, values, flags).ConfigureAwait(false);
                return result;
            }
            catch (RedisException ex)
            {
                redisException = ex;
                throw;
            }
            catch (RedisTimeoutException ex)
            {
                timeoutException = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                TrackDependency(database, telemetryClient, startTime, stopwatch.Elapsed, "Script evaluate (EVAL)", result == null ? ResultType.Error : result.Type, redisException, timeoutException);
            }
        }

        /// <summary>
        ///  Removes the specified key. A key is ignored if it does not exist.
        /// </summary>
        /// <param name="database">Redis instance.</param>
        /// <param name="key">The key to delete.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <returns>True if the key was removed.</returns>
        public static async Task<bool> KeyDeleteAsync(this IDatabase database, RedisKey key, CommandFlags flags = CommandFlags.None, TelemetryClient telemetryClient = null)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            RedisException redisException = null;
            RedisTimeoutException timeoutException = null;

            try
            {
                bool result = await database.KeyDeleteAsync(key).ConfigureAwait(false);
                return result;
            }
            catch (RedisException ex)
            {
                redisException = ex;
                throw;
            }
            catch (RedisTimeoutException ex)
            {
                timeoutException = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                TrackDependency(database, telemetryClient, startTime, stopwatch.Elapsed, "Key delete (DEL)", ResultType.None, redisException, timeoutException);
            }
        }

        /// <summary>
        ///  Track an Redis Operation in Application Insights.
        /// </summary>
        /// <param name="database">Redis instance.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="now">Current Time.</param>
        /// <param name="duration">Duration.</param>
        /// <param name="operation">Operation Executed.</param>
        /// <param name="resultType">Result.</param>
        /// <param name="redisException">RedisException.</param>
        /// <param name="timeoutException">RedisTimeoutException.</param>
        private static void TrackDependency(IDatabase database, TelemetryClient telemetryClient, DateTimeOffset now, TimeSpan duration, string operation, ResultType resultType, RedisException redisException = null, RedisTimeoutException timeoutException = null)
        {
            if (telemetryClient == null)
            {
                // This service doesn't have Application Insights
                return;
            }

#if !(DEBUG || SANDBOX)
            if (redisException == null && timeoutException == null)
            {
                // Per Pavan's request only track dependencies that have an exception
                // to reduce the amount of AppInsights traces.
                return;
            }
#endif

            var status = database.Multiplexer.GetStatus();
            var counters = database.Multiplexer.GetCounters();
            var target = database.Multiplexer.Configuration.Split(',')[0];

            var telemetry = new DependencyTelemetry(
                dependencyName: operation,
                target: target,
                dependencyTypeName: "Distributed Cache",
                data: "db" + database.Database,
                startTime: now,
                duration: duration,
                resultCode: resultType.ToString(),
                success: redisException == null && timeoutException == null);

            telemetry.Properties["Status"] = status;
            telemetry.Properties["Is connected"] = database.Multiplexer.IsConnected.ToString(CultureInfo.InvariantCulture);
            telemetry.Metrics["Total outstanding"] = counters.TotalOutstanding;

            if (redisException != null)
            {
                telemetry.Properties["Message"] = redisException.Message;
            }
            else if (timeoutException != null)
            {
                telemetry.Properties["Message"] = timeoutException.Message;
            }

            telemetryClient.TrackDependency(telemetry);
        }
    }
}
