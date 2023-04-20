namespace Exos.Platform.ICTLibrary.Repository
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Dapper;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.ICTLibrary.Repository.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc/>
    public class IctRepository : IIctRepository
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IctRepository> _logger;
        private readonly IMemoryCache _memoryCache;

        private IctContext _ictContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="IctRepository"/> class.
        /// </summary>
        /// <param name="memoryCache">IMemoryCache.</param>
        /// <param name="serviceProvider">IServiceProvider.</param>
        /// <param name="logger">ILogger.</param>
        public IctRepository(IMemoryCache memoryCache, IServiceProvider serviceProvider, ILogger<IctRepository> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <inheritdoc/>
        public async Task<EventEntityTopic> GetEventEntityTopic(string entityName, string eventName, short applicationId)
        {
            _ictContext = _serviceProvider.GetService<IctContext>();
            string keyName = entityName + "_" + eventName + "_" + applicationId;

            // var objStringFromCache = await _memoryCache.Get  _distributedCache.GetStringAsync(keyName).ConfigureAwait(false);
            if (_memoryCache.TryGetValue(keyName, out string objStringFromCache))
            {
                var cachedObj = JsonSerializer.Deserialize<EventEntityTopicCache>(objStringFromCache);
                var entityTopic = ReflectionHelper.Map<EventEntityTopicCache, EventEntityTopic>(cachedObj);
                return entityTopic;
            }

            var eventTopic = await _ictContext.EventEntityTopic.Include(p => p.Publisher)
                .FirstOrDefaultAsync(a => a.EntityName == entityName && a.EventName == eventName && a.PublisherId == applicationId).ConfigureAwait(false);
            var cachObj = ReflectionHelper.Map<EventEntityTopic, EventEntityTopicCache>(eventTopic);
            if (eventTopic != null)
            {
                cachObj.PublisherName = eventTopic.Publisher.EventPublisherName;
                var cacheExpirationOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(120),
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = TimeSpan.FromMinutes(120 - 2)
                };

                _memoryCache.Set(keyName, JsonSerializer.Serialize(cachObj), cacheExpirationOptions);
                // await _distributedCache.SetStringAsync(keyName, System.Text.Json.JsonSerializer.Serialize(cachObj)).ConfigureAwait(false);
            }

            return eventTopic;
        }

        /// <inheritdoc/>
        public async Task<EventEntityTopic> GetEventEntityTopic(string entityName, string eventName, string applicationName)
        {
            _ictContext = _serviceProvider.GetService<IctContext>();

            // Get it from the cache. if not get it from the db.
            var keyName = entityName + "_" + eventName + "_" + applicationName;
            // var objStringFromCache = await _distributedCache.GetStringAsync(keyName).ConfigureAwait(false);
            if (_memoryCache.TryGetValue(keyName, out string objStringFromCache))
            {
                var cachedObj = JsonSerializer.Deserialize<EventEntityTopicCache>(objStringFromCache);
                var entityTopic = ReflectionHelper.Map<EventEntityTopicCache, EventEntityTopic>(cachedObj);
                entityTopic.Publisher = new EventPublisher
                {
                    EventPublisherName = cachedObj.PublisherName,
                    EventPublisherId = cachedObj.PublisherId,
                };
                return entityTopic;
            }

            var eventTopic = await _ictContext.EventEntityTopic.Include(p => p.Publisher)
                .FirstOrDefaultAsync(a => a.EntityName == entityName && a.EventName == eventName && a.Publisher.EventPublisherName == applicationName);
            if (eventTopic != null)
            {
                var toCacheObj = ReflectionHelper.Map<EventEntityTopic, EventEntityTopicCache>(eventTopic);
                toCacheObj.PublisherName = eventTopic.Publisher.EventPublisherName;

                var cacheExpirationOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(120),
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = TimeSpan.FromMinutes(120 - 2)
                };

                _memoryCache.Set(keyName, JsonSerializer.Serialize(toCacheObj), cacheExpirationOptions);
                // await _distributedCache.SetStringAsync(keyName, System.Text.Json.JsonSerializer.Serialize(toCacheObj)).ConfigureAwait(false);
            }

            // not returning the same objects. it is OK for now, only few fields
            return eventTopic;
        }

        /// <inheritdoc/>
        public async Task AddEventTracking(EventTracking eventTracking)
        {
            if (eventTracking == null)
            {
                throw new ArgumentNullException(nameof(eventTracking));
            }

            _ictContext = _serviceProvider.GetService<IctContext>();
            // _logger.LogDebug($"Saving tracking data  {LoggerHelper.SanitizeValue(eventTracking)}");
            var connection = _ictContext.Database.GetDbConnection();
            var insertQuery = @"INSERT INTO [ict].[EventTracking]([TrackingId], [EventName], [EntityName], [ApplicationName], [TopicName], [CreatedDate],[CreatedBy],[LastUpdatedDate],[LastUpdatedBy])  VALUES  (@TrackingId, @EventName, @EntityName, @ApplicationName, @TopicName, @CreatedDate,@CreatedBy,@LastUpdatedDate,@LastUpdatedBy)";

            var savedEntity = await connection.ExecuteAsync(insertQuery, new
            {
                TrackingId = eventTracking.TrackingId,
                EventName = eventTracking.EventName,
                EntityName = eventTracking.EntityName,
                ApplicationName = eventTracking.ApplicationName,
                TopicName = eventTracking.TopicName,
                CreatedDate = eventTracking.CreatedDate,
                CreatedBy = eventTracking.CreatedBy,
                LastUpdatedDate = eventTracking.LastUpdatedDate,
                LastUpdatedBy = eventTracking.LastUpdatedBy,
            }).ConfigureAwait(false);
        }
    }
}
