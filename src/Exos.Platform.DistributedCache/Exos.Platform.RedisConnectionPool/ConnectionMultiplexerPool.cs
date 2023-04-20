using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Exos.Platform.RedisConnectionPool
{
    /// <summary>
    /// An implementation of the <see cref="IConnectionMultiplexerPool" /> interface.
    /// </summary>
    public class ConnectionMultiplexerPool : IConnectionMultiplexerPool
    {
        private readonly List<Lazy<Task<IConnectionMultiplexer>>> _connections;
        private readonly int _poolSize;
        private readonly ConnectionMultiplexerPoolOptions _options;
        private readonly TelemetryClient _telemetryClient;
        private volatile int _lastIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionMultiplexerPool" /> class.
        /// </summary>
        /// <param name="optionsAccessor">An instance of <see cref="ConnectionMultiplexerPoolOptions" />.</param>
        /// <param name="telemetryClient">Optional. A <see cref="TelemetryClient" /> for tracking dependency calls and exceptions.</param>
        public ConnectionMultiplexerPool(IOptions<ConnectionMultiplexerPoolOptions> optionsAccessor, TelemetryClient telemetryClient = null)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;
            _telemetryClient = telemetryClient;
            _poolSize = _options.PoolSize;

            if (_poolSize < 1)
            {
                _poolSize = 1;
            }

            // Create the connection pool
            _connections = new List<Lazy<Task<IConnectionMultiplexer>>>();
            for (int i = 0; i < _poolSize; i++)
            {
                _connections.Add(CreateConnection(i));
            }
        }

        /// <summary>
        /// Retrieves a multiplexer connection from the pool.
        /// </summary>
        /// <returns>An established <see cref="Task{IConnectionMultiplexer}" /> from the pool.</returns>
        public Task<IConnectionMultiplexer> GetConnectionAsync()
        {
            Task<IConnectionMultiplexer> connection = null;

            // Loop to avoid any transient errors with our dirty read
            while (connection == null)
            {
                // Round-robin the next connection
                var index = GetNextIndex();
                connection = _connections[index].Value;
            }

            return connection;
        }

        /// <summary>
        /// Removes a failed multiplexer connection from the pool.
        /// A new connection will be created to replace the failed one.
        /// </summary>
        /// <param name="connection">A failed <see cref="IConnectionMultiplexer" />.</param>
        /// <param name="exception">The exception that caused the connection to fail.</param>
        public void FailConnection(IConnectionMultiplexer connection, Exception exception)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (_telemetryClient != null)
            {
                var counters = connection.GetCounters();
                var status = connection.GetStatus();

                var telemetry = new ExceptionTelemetry(exception)
                {
                    SeverityLevel = SeverityLevel.Warning,
                    Message = $"An {nameof(IConnectionMultiplexer)} in the {nameof(IConnectionMultiplexerPool)} failed and has to be replaced. {exception?.Message}"
                };
                telemetry.Properties["Status"] = status;
                telemetry.Properties["Is connected"] = connection.IsConnected.ToString(CultureInfo.InvariantCulture);
                telemetry.Metrics["Total outstanding"] = counters.TotalOutstanding;
                telemetry.Metrics["Pool size"] = _connections.Count;

                _telemetryClient.TrackException(telemetry);
            }

            ReplaceConnection(connection);
        }

        /// <summary>
        /// Create a Connection.
        /// </summary>
        /// <param name="poolIndex">Position in the connection pool.</param>
        /// <returns>A Connection.</returns>
        private Lazy<Task<IConnectionMultiplexer>> CreateConnection(int poolIndex)
        {
            return new Lazy<Task<IConnectionMultiplexer>>(async () =>
            {
                var startTime = DateTimeOffset.UtcNow;
                var stopwatch = Stopwatch.StartNew();

                var connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration).ConfigureAwait(false);

                stopwatch.Stop();

                if (_telemetryClient != null)
                {
                    var status = connection.GetStatus();
                    var counters = connection.GetCounters();
                    var target = connection.Configuration.Split(',')[0];

                    var telemetry = new DependencyTelemetry(
                        dependencyName: "Connect",
                        target: target,
                        dependencyTypeName: "Connection Pool",
                        data: target,
                        startTime: startTime,
                        duration: stopwatch.Elapsed,
                        resultCode: ResultType.None.ToString(),
                        success: true);

                    telemetry.Properties["Status"] = status;
                    telemetry.Properties["Is connected"] = connection.IsConnected.ToString(CultureInfo.InvariantCulture);
                    telemetry.Metrics["Pool index"] = poolIndex;
                    telemetry.Metrics["Total outstanding"] = counters.TotalOutstanding;

                    _telemetryClient.TrackDependency(telemetry);
                }

                return connection;
            });
        }

        /// <summary>
        /// Replace a failed Connection.
        /// </summary>
        /// <param name="failedConnection">Failed Connection.</param>
        private void ReplaceConnection(IConnectionMultiplexer failedConnection)
        {
            // Find the failed connection in the pool
            var index = _connections.FindIndex(t =>
            {
                return t.IsValueCreated && t.Value.IsCompleted && t.Value.Result == failedConnection;
            });

            if (index >= 0)
            {
                // Replace with a new connection
                _connections[index] = CreateConnection(index);

                // A cheap way to avoid locking is to just allow time for any callers to finish
                // who may have gotten this connection before we pulled it out of the pool. i.e.
                // give the connection 2 minutes to finish any calls in progress.
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                    await failedConnection.CloseAsync(true).ConfigureAwait(false);

                    if (_telemetryClient != null)
                    {
                        var telemetry = new TraceTelemetry
                        {
                            SeverityLevel = SeverityLevel.Information,
                            Message = $"A failed {nameof(IConnectionMultiplexer)} in the {nameof(IConnectionMultiplexerPool)} has now been closed."
                        };
                        telemetry.Properties["Pool size"] = _connections.Count.ToString(CultureInfo.InvariantCulture);
                        telemetry.Properties["Pool index"] = index.ToString(CultureInfo.InvariantCulture);

                        _telemetryClient.TrackTrace(telemetry);
                    }
                });
            }
        }

        /// <summary>
        /// Get next index in the pool.
        /// </summary>
        /// <returns>Next index.</returns>
        private int GetNextIndex()
        {
            uint nextNumber = unchecked((uint)System.Threading.Interlocked.Increment(ref _lastIndex));
            int result = (int)(nextNumber % _poolSize);
            return result;
        }
    }
}
