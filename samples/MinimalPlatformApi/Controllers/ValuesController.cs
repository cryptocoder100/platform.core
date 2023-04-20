namespace Exos.MinimalPlatformApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.MinimalPlatformApi.Models;
    using Exos.MinimalPlatformApi.Repositories;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Models;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The API for the values collection.
    /// </summary>
    [AllowAnonymous]
    [Route("api/v1/[controller]")]
    public class ValuesController : ControllerBase
    {
        private readonly IValuesRepository _repository;
        private readonly INamedServiceFactory<IValuesRepository> _namedRepositories;
        private readonly ILogger<ValuesController> _logger;
        private readonly IUserContextAccessor _userContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValuesController"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="namedRepositories">The named repositories.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userContextAccessor">The user context accessor.</param>
        public ValuesController(IValuesRepository repository, INamedServiceFactory<IValuesRepository> namedRepositories, ILogger<ValuesController> logger, IUserContextAccessor userContextAccessor)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _namedRepositories = namedRepositories ?? throw new ArgumentNullException(nameof(namedRepositories));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContextAccessor = userContextAccessor ?? throw new ArgumentNullException(nameof(userContextAccessor));
        }

        /// <summary>
        /// Query the values collection.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ListModel<ValueModel>), 200)]
        public async Task<IActionResult> Get(ValueQueryModel query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (!ModelState.IsValid)
            {
                throw new BadRequestException(ModelState);
            }

            try
            {
                _logger.LogDebug("Query: Name={Name}, Value={Value}, Token={Token}", query.NameContains, query.ValueContains, query.ContinuationToken);

                var queriedValues = await _repository.QueryValuesAsync(query).ConfigureAwait(false);

                _logger.LogInformation("Query result: Count={Count}, Token={Token}", queriedValues.Data.Count, queriedValues.ContinuationToken);

                _logger.LogInformation($"User is Manager:{_userContextAccessor.IsManager}");
                _logger.LogInformation($"User is ExosAdmin:{_userContextAccessor.ExosAdmin}");

                return Ok(queriedValues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Query error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieve a value from the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to retrieve.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ValueModel), 200)]
        public async Task<IActionResult> Get(string id)
        {
            TelemetryHelper.TryEnrichRequestTelemetry(HttpContext, KeyValuePair.Create("id", id));

            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id is empty.", nameof(id));
            }

            try
            {
                _logger.LogDebug("Read: Id={Id}", id);
                _logger.LogInformation("Test1={Test1}, Test2={Test2}", null, LoggerHelper.SanitizeValue((object)null));

                var readValue = await _repository.ReadValueAsync(id).ConfigureAwait(false);
                if (readValue == null)
                {
                    throw new NotFoundException(nameof(id), $"A value with an identification of {id} was not found.");
                }
                else
                {
                    _logger.LogInformation("Read result: {Name} = {Value}", readValue.Name, readValue.Value);
                }

                return Ok(readValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Read error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieve a value from the values collection.
        /// </summary>
        /// <param name="name">Service name.</param>
        /// <param name="id">The identification of the value to retrieve.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("{name}/{id}")]
        [ProducesResponseType(typeof(ValueModel), 200)]
        public async Task<IActionResult> Get(string name, string id)
        {
            var repository = _namedRepositories.GetService(name);
            var readValue = await repository.ReadValueAsync(id).ConfigureAwait(false);

            return Ok(readValue);
        }

        /// <summary>
        /// Insert a value into the values collection.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ValueModel), 200)]
        public async Task<IActionResult> Post([FromBody] ValueModel value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!ModelState.IsValid)
            {
                throw new BadRequestException(ModelState);
            }

            try
            {
                _logger.LogDebug("Create: {Name}={Value}", value.Name, value.Value);

                var createdValue = await _repository.CreateValueAsync(value).ConfigureAwait(false);

                _logger.LogInformation("Create result: Id={Id}", createdValue.Id);

                return Ok(createdValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Create error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Insert a value into the values collection.
        /// </summary>
        /// <param name="name">Service name.</param>
        /// <param name="value">The value to insert.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost("{name}")]
        [ProducesResponseType(typeof(ValueModel), 200)]
        public async Task<IActionResult> Post(string name, [FromBody] ValueModel value)
        {
            var repository = _namedRepositories.GetService(name);
            var createdValue = await repository.CreateValueAsync(value).ConfigureAwait(false);

            return Ok(createdValue);
        }

        /// <summary>
        /// Replace a value in the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to replace.</param>
        /// <param name="value">The value to replace with.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ValueModel), 200)]
        public async Task<IActionResult> Put(string id, [FromBody] ValueModel value)
        {
            TelemetryHelper.TryEnrichRequestTelemetry(HttpContext, KeyValuePair.Create("id", id));

            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id is empty.", nameof(id));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!ModelState.IsValid)
            {
                throw new BadRequestException(ModelState);
            }

            try
            {
                _logger.LogDebug("Update: Id={Id}, {Name}={Value}", id, value.Name, value.Value);

                var updatedValue = await _repository.UpdateValueAsync(id, value).ConfigureAwait(false);
                if (updatedValue == null)
                {
                    throw new NotFoundException(nameof(id), $"A value with an identification of {id} was not found.");
                }
                else
                {
                    _logger.LogInformation("Update result: Id={Id}, {Name}={Value}", updatedValue.Id, updatedValue.Name, updatedValue.Value);
                }

                return Ok(updatedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Update error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Remove a value from the values collection.
        /// </summary>
        /// <param name="id">The identification of the value to remove.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            TelemetryHelper.TryEnrichRequestTelemetry(HttpContext, KeyValuePair.Create("id", id));

            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id is empty.", nameof(id));
            }

            try
            {
                _logger.LogDebug("Delete: Id={Id}", id);

                var deleted = await _repository.DeleteValueAsync(id).ConfigureAwait(false);
                if (!deleted)
                {
                    throw new NotFoundException(nameof(id), $"A value with an identification of {id} was not found.");
                }
                else
                {
                    _logger.LogInformation("Delete result: Deleted");
                }

                return Ok(deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Delete error: {ex.Message}");
                throw;
            }
        }
    }
}
