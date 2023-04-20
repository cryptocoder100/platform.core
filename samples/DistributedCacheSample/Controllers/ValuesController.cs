#pragma warning disable CA1308

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace DistributedCacheSample.Controllers
{
    [ApiController]
    [Route("api/v1/values")]
    [Produces("application/json")]
    public class ValuesController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _options;

        public ValuesController(IDistributedCache cache)
        {
            _cache = cache;
            _options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var item = await _cache.GetStringAsync(id, HttpContext.RequestAborted);
            if (item == null)
            {
                return Ok(null);
            }

            using var jsonDocument = JsonDocument.Parse(item);
            return Ok(jsonDocument.RootElement.Clone());
        }

        [HttpPost("")]
        public async Task<IActionResult> Post([FromBody] JsonElement value)
        {
            var id = Guid.NewGuid().ToString().ToLowerInvariant();
            await _cache.SetStringAsync(
                id,
                JsonSerializer.Serialize(value, _options),
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)),
                HttpContext.RequestAborted);

            return Ok(new { Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] JsonElement value)
        {
            await _cache.SetStringAsync(
               id,
               JsonSerializer.Serialize(value, _options),
               new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)),
               HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _cache.RemoveAsync(
               id,
               HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
