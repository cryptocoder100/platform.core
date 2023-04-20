#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler

namespace Exos.MinimalPlatformApi.Repositories
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Exos.MinimalPlatformApi.Models;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An in-memory implementation of the interface to the values collection.
    /// </summary>
    public class ValuesMemoryRepository : IValuesRepository
    {
        private readonly ILogger<ValuesMemoryRepository> _logger;
        private readonly ConcurrentDictionary<string, ValueModel> _values;
        private readonly ConcurrentDictionary<string, List<ValueModel>> _continuations;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValuesMemoryRepository"/> class.
        /// </summary>
        /// <param name="logger">Logger Instance.</param>
        public ValuesMemoryRepository(ILogger<ValuesMemoryRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create a starter set of values
            _values = new ConcurrentDictionary<string, ValueModel>();
            for (var i = 0; i < 100; i++)
            {
                var value = new ValueModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Name #{i}",
                    Value = i.ToString(CultureInfo.InvariantCulture),
                };
                _values.TryAdd(value.Id, value);
            }

            // Create a storage area for continuations
            _continuations = new ConcurrentDictionary<string, List<ValueModel>>();
        }

        /// <summary>
        /// Insert a value into the values collection.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>The value inserted with the identification updated.</returns>
        public Task<ValueModel> CreateValueAsync(ValueModel value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!string.IsNullOrWhiteSpace(value.Id))
            {
                _logger.LogWarning("The value's id property will be overwritten");
            }

            var id = Guid.NewGuid().ToString();
            value.Id = id;

            if (!_values.TryAdd(id, value))
            {
                throw new InvalidOperationException("Unable to add the value model");
            }

            return Task.FromResult(value);
        }

        /// <summary>
        /// Retrieve a value from the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to retrieve.</param>
        /// <returns>The value retrieved, or null if not found.</returns>
        public Task<ValueModel> ReadValueAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            _values.TryGetValue(id, out ValueModel value);

            return Task.FromResult(value);
        }

        /// <summary>
        /// Replace a value in the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to replace.</param>
        /// <param name="value">The value to replace with.</param>
        /// <returns>The replaced value with the identification updated, or null if not found.</returns>
        public Task<ValueModel> UpdateValueAsync(string id, ValueModel value)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!string.IsNullOrWhiteSpace(value.Id) && value.Id.Trim() != id.Trim())
            {
                _logger.LogWarning("The value's id property will be overwritten");
            }

            value.Id = id;

            ValueModel oldValue;
            do
            {
                if (!_values.TryGetValue(id, out oldValue))
                {
                    return Task.FromResult((ValueModel)null);
                }
            }
            while (_values.TryUpdate(id, value, oldValue));

            return Task.FromResult(value);
        }

        /// <summary>
        /// Remove a value from the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to remove.</param>
        /// <returns>True if the value was removed, or false if not found.</returns>
        public Task<bool> DeleteValueAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var deleted = _values.TryRemove(id, out ValueModel oldValue);

            return Task.FromResult(deleted);
        }

        /// <summary>
        /// Query the values collection.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The query results contained in a ListModel.</returns>
        public Task<ListModel<ValueModel>> QueryValuesAsync(ValueQueryModel query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            List<ValueModel> valuesFound;
            if (!string.IsNullOrWhiteSpace(query.ContinuationToken))
            {
                // Use the results from the previous query
                if (!_continuations.TryRemove(query.ContinuationToken, out valuesFound))
                {
                    throw new ArgumentOutOfRangeException(nameof(query), "Invalid continuation token");
                }
            }
            else
            {
                // Construct a new query
                var valuesQuery = _values.Select(item => item.Value);

                if (!string.IsNullOrWhiteSpace(query.NameContains))
                {
                    valuesQuery = valuesQuery.Where(item => item.Name.Contains(query.NameContains.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(query.ValueContains))
                {
                    valuesQuery = valuesQuery.Where(item => item.Name.Contains(query.NameContains.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                switch (query.OrderedBy)
                {
                    case ValueQueryModel.OrderBy.Id:
                        valuesQuery = valuesQuery.OrderBy(item => item.Id);
                        break;

                    case ValueQueryModel.OrderBy.Name:
                        valuesQuery = valuesQuery.OrderBy(item => item.Name);
                        break;

                    case ValueQueryModel.OrderBy.Value:
                        valuesQuery = valuesQuery.OrderBy(item => item.Value);
                        break;
                }

                valuesFound = valuesQuery.ToList();
            }

            // Prepare the results
            var queriedValues = new ListModel<ValueModel>();
            queriedValues.Data = valuesFound.Take(query.Limit).ToList();

            // Create a continuation if the found count exceeds the limit
            if (valuesFound.Count > query.Limit)
            {
                var token = Guid.NewGuid().ToString();
                queriedValues.ContinuationToken = token;

                List<ValueModel> valuesContinuation = valuesFound.Skip(query.Limit).ToList();

                if (!_continuations.TryAdd(token, valuesContinuation))
                {
                    throw new InvalidOperationException("Internal logic error: unable to add continuation");
                }

                // Remove the continuation if not exercised within 5 minutes
                Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(state =>
                {
                    List<ValueModel> discardedContinuation;
                    _continuations.TryRemove(token, out discardedContinuation);
                });
            }

            return Task.FromResult(queriedValues);
        }
    }
}
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler