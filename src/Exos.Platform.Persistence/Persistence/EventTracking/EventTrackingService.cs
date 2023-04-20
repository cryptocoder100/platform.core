namespace Exos.Platform.Persistence.EventTracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Persistence.Entities;
    using Exos.Platform.Persistence.Models;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Exos.Platform.TenancyHelper.MultiTenancy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Service to process tracking events.
    /// </summary>
    /// <typeparam name="T">Event Type.</typeparam>
    public class EventTrackingService<T> : IEventTrackingService where T : EventTrackingEntity, new()
    {
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly ILogger<EventTrackingService<T>> _logger;
        private readonly IEventSqlServerRepository<T> _eventSqlServerRepository;
        private readonly FutureEventsConfig _futureEventsConfig;
        private readonly ServiceConfig _serviceConfig;
        private readonly ImplicitEventsConfig _implicitEventsConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTrackingService{T}"/> class.
        /// </summary>
        /// <param name="userHttpContextAccessorService"><see cref="IUserHttpContextAccessorService"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="eventSqlServerRepository"><see cref="IEventSqlServerRepository{T}"/>.</param>
        /// <param name="futureEventsOptions"><see cref="FutureEventsConfig"/>.</param>
        /// <param name="serviceConfigOptions"><see cref="ServiceConfig"/>.</param>
        /// <param name="implicitEventsConfigOptions"><see cref="ImplicitEventsConfig"/>.</param>
        public EventTrackingService(IUserHttpContextAccessorService userHttpContextAccessorService, ILogger<EventTrackingService<T>> logger, IEventSqlServerRepository<T> eventSqlServerRepository, IOptions<FutureEventsConfig> futureEventsOptions, IOptions<ServiceConfig> serviceConfigOptions, IOptions<ImplicitEventsConfig> implicitEventsConfigOptions)
        {
            _userHttpContextAccessorService = userHttpContextAccessorService;
            _logger = logger;
            _eventSqlServerRepository = eventSqlServerRepository;
            _futureEventsConfig = futureEventsOptions != null ? futureEventsOptions.Value : null;
            _serviceConfig = serviceConfigOptions != null ? serviceConfigOptions.Value : null;
            _implicitEventsConfig = implicitEventsConfigOptions != null && implicitEventsConfigOptions.Value != null ? implicitEventsConfigOptions.Value : null;
        }

        private Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Create a list of events.
        /// </summary>
        /// <param name="platformDbContext"><see cref="PlatformDbContext"/>.</param>
        /// <returns>List of events.</returns>
        public List<EventTrackingEntry> CreateEventTracking(PlatformDbContext platformDbContext)
        {
            if (platformDbContext is null)
            {
                throw new ArgumentNullException(nameof(platformDbContext));
            }

            List<EventTrackingEntry> eventTrackingEntryList = new List<EventTrackingEntry>();

            foreach (EntityEntry<IAuditable> auditableEntityEntry in platformDbContext.ChangeTracker.Entries<IAuditable>())
            {
                if (auditableEntityEntry.State == EntityState.Detached || auditableEntityEntry.State == EntityState.Unchanged)
                {
                    continue;
                }

                // Check if entity is decorated to required Event Tracking
                EventTracking eventTrackingAttribute = (EventTracking)Attribute.GetCustomAttribute(auditableEntityEntry.Entity.GetType(), typeof(EventTracking));

                // Check if we need to enable EventTracking
                if (eventTrackingAttribute != null)
                {
                    switch (auditableEntityEntry.State)
                    {
                        case EntityState.Added:
                            if (eventTrackingAttribute.Add)
                            {
                                EventTrackingEntry eventTrackingEntry = CreateEventTrackingEntry(auditableEntityEntry);
                                eventTrackingEntryList.Add(eventTrackingEntry);
                            }

                            break;
                        case EntityState.Modified:
                            // Check if is a soft delete first, the isActive flag is set to false
                            if (!auditableEntityEntry.Entity.IsActive)
                            {
                                if (eventTrackingAttribute.Delete)
                                {
                                    EventTrackingEntry eventTrackingEntry = CreateEventTrackingEntry(auditableEntityEntry);
                                    eventTrackingEntryList.Add(eventTrackingEntry);
                                }
                            }
                            else if (eventTrackingAttribute.Modify)
                            {
                                EventTrackingEntry eventTrackingEntry = CreateEventTrackingEntry(auditableEntityEntry);
                                eventTrackingEntryList.Add(eventTrackingEntry);
                            }

                            break;
                        case EntityState.Deleted:
                            if (eventTrackingAttribute.Delete)
                            {
                                EventTrackingEntry eventTrackingEntry = CreateEventTrackingEntry(auditableEntityEntry);
                                eventTrackingEntryList.Add(eventTrackingEntry);
                            }

                            break;
                    }
                }
            }

            string currentUser = GetCurrentUser();
            // Save entities that have all the values set first ( don't have keys generated by the db)
            foreach (EventTrackingEntry trackingEntry in eventTrackingEntryList.Where(cte => !cte.HasTemporaryProperties))
            {
                if (_serviceConfig != null && !string.IsNullOrEmpty(_serviceConfig.PublisherName))
                {
                    trackingEntry.PublisherName = _serviceConfig.PublisherName;
                }

                EventTrackingEntity eventTrackingEntity = trackingEntry.CreateEntity<T>(_implicitEventsConfig);
                eventTrackingEntity.IsActive = true;
                eventTrackingEntity.CreatedBy = !string.IsNullOrEmpty(currentUser) ? currentUser : "Service running without user context"; // For example, consumer auth generates Mfa event, its running w/o user context; may be, we run that under system account?
                eventTrackingEntity.CreatedDate = DateTime.UtcNow;

                platformDbContext.Add(eventTrackingEntity);
            }

            // Return a list of entities that don't have values set (usually keys generated in the db)
            return eventTrackingEntryList.Where(cte => cte.HasTemporaryProperties).ToList();
        }

        /// <summary>
        /// Update a list of events asynchronously.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events.</param>
        /// <param name="platformDbContext"><see cref="PlatformDbContext"/>.</param>
        /// <returns>Updated events.</returns>
        public Task UpdateEventTrackingAsync(List<EventTrackingEntry> eventTrackingEntryList, PlatformDbContext platformDbContext)
        {
            if (eventTrackingEntryList == null || eventTrackingEntryList.Count == 0)
            {
                return Task.CompletedTask;
            }

            if (platformDbContext is null)
            {
                throw new ArgumentNullException(nameof(platformDbContext));
            }

            EvaluateEventTrackingList(eventTrackingEntryList, platformDbContext);
            return platformDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Update a list of events synchronously.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events.</param>
        /// <param name="platformDbContext"><see cref="PlatformDbContext"/>.</param>
        /// <returns>Updated events.</returns>
        public int UpdateEventTracking(List<EventTrackingEntry> eventTrackingEntryList, PlatformDbContext platformDbContext)
        {
            if (eventTrackingEntryList == null || eventTrackingEntryList.Count == 0)
            {
                return 0;
            }

            if (platformDbContext is null)
            {
                throw new ArgumentNullException(nameof(platformDbContext));
            }

            EvaluateEventTrackingList(eventTrackingEntryList, platformDbContext);
            return platformDbContext.SaveChanges();
        }

        /// <summary>
        /// Create an Explicit Event.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <param name="eventConfig">Event Config.</param>
        /// <returns>Event Tracking Entity.</returns>
        public async Task<EventTrackingEntity> CreateExplicitEvent(ExplicitEvent explictEvent, EventConfig eventConfig)
        {
            if (explictEvent is null)
            {
                throw new ArgumentNullException(nameof(explictEvent));
            }

            // Create the event
            EventTrackingEntity createdEvent = await CreateEventImpl(explictEvent).ConfigureAwait(false);

            // Generate future events
            if (explictEvent.FutureEventCtx != null)
            {
                if (!explictEvent.FutureEventCtx.DisableFutureEventGeneration)
                {
                    if (explictEvent.FutureEventCtx != null && eventConfig != null)
                    {
                        _logger.LogDebug("Processing the following future configuration = {eventConfig}, processing event = {event} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(eventConfig), LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                        await CancelFutureEvents(explictEvent, eventConfig).ConfigureAwait(false);
                        await GenerateFutureEvents(explictEvent, eventConfig).ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogDebug("Future event generation is disabled as requested for explicit event = {eventName} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                }
            }

            return createdEvent;
        }

        /// <summary>
        /// Create explicit (not persistence) events.
        /// </summary>
        /// <param name="explictEvents">Events.</param>
        /// <returns>List of EventTrackingEntity.</returns>
        public async Task<List<EventTrackingEntity>> CreateExplicitEvents(List<ExplicitEvent> explictEvents)
        {
            if (explictEvents == null)
            {
                throw new ArgumentNullException(nameof(explictEvents));
            }

            // Add Custom Header or X-Forwarded-Host (EncryptionConstants.EncryptionRequestHeader) used as key identifier for encryption/decryption.
            explictEvents.ForEach(explictEvent => AddEncryptionRequestHeaderToEvent(explictEvent));

            string currentUser = GetCurrentUser();
            List<T> eventTrackingEntities = explictEvents.Select(explictEvent => new T()
            {
                PrimaryKeyValue = explictEvent.PrimaryKeyValue,
                EntityName = explictEvent.EntityName,
                EventName = explictEvent.EventName,
                Payload = explictEvent.Payload,
                Metadata = explictEvent.Metadata,
                PublisherName = explictEvent.PublisherName,
                PublisherId = explictEvent.PublisherId,
                TrackingId = GetTrackingId(),
                IsActive = true,
                CreatedBy = !string.IsNullOrEmpty(currentUser) ? currentUser : "Service running without user context", // For example, consumer auth generates Mfa event, its running w/o user context; may be, we run that under system account?
                CreatedDate = DateTime.UtcNow,
                UserContext = GetUserContext(),
                DueDate = explictEvent.DueDate,
            }).ToList();

            // Save Event
            List<T> createdEvents = await _eventSqlServerRepository.CreateEvents(eventTrackingEntities).ConfigureAwait(false);
            return createdEvents.ToList<EventTrackingEntity>();
        }

        /// <summary>
        /// Create an explicit (not persistence) event.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <returns>Event Tracking Entity.</returns>
        public async Task<EventTrackingEntity> CreateExplicitEvent(ExplicitEvent explictEvent)
        {
            if (explictEvent is null)
            {
                throw new ArgumentNullException(nameof(explictEvent));
            }

            // Create the event which is requested.
            EventTrackingEntity createdEvent = await CreateEventImpl(explictEvent).ConfigureAwait(false);

            if (explictEvent.FutureEventCtx != null)
            {
                // Generate Future events for the event which is requested.
                if (!explictEvent.FutureEventCtx.DisableFutureEventGeneration)
                {
                    if (explictEvent.FutureEventCtx != null && _futureEventsConfig != null && _futureEventsConfig.EventsConfig != null && _futureEventsConfig.EventsConfig.Count > 0)
                    {
                        _logger.LogDebug("Found the following future configuration = {futureEventConfig}, processing event = {event} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(_futureEventsConfig), LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                        EventConfig futureEventConfig = _futureEventsConfig.EventsConfig.Find(fe => fe.SrcEvent == explictEvent.EventName);
                        await CancelFutureEvents(explictEvent, futureEventConfig).ConfigureAwait(false);
                        await GenerateFutureEvents(explictEvent, futureEventConfig).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogDebug("Future event is not generated for the event = {event}, no configuration found for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                    }
                }
                else
                {
                    _logger.LogDebug("Future event generation is disabled as requested for explicit event = {eventName} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                }
            }

            return createdEvent;
        }

        /// <summary>
        /// Get Property Value.
        /// </summary>
        /// <param name="src">Souce object.</param>
        /// <param name="propName">Property name to find in object.</param>
        /// <returns>Property Value.</returns>
        public object GetPropValue(object src, string propName)
        {
            if (src == null)
            {
                return null;
            }

            PropertyInfo property = src.GetType().GetProperty(propName);

            if (property == null)
            {
                _logger.LogError("PropertyName = " + propName + " does not exists on object with type = " + src.GetType().ToString());
                throw new ExosPersistenceException("PropertyName = " + propName + " does not exists on object with type = " + src.GetType().ToString());
            }

            return property.GetValue(src, null);
        }

        /// <summary>
        /// Generate Future Event.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <param name="futureEvent">Future Event.</param>
        /// <returns>EventTrackingEntity.</returns>
        public async Task<EventTrackingEntity> GenerateFutureEvent(ExplicitEvent explictEvent, FutureEvent futureEvent)
        {
            if (explictEvent is null)
            {
                return null;
            }

            if (futureEvent is null)
            {
                return null;
            }

            DateTime? dueDate = GetDueDate(explictEvent, futureEvent);
            if (dueDate != null && dueDate.HasValue)
            {
                if (dueDate >= DateTime.UtcNow)
                {
                    ExplicitEvent explictEventToCreate = new ExplicitEvent()
                    {
                        PrimaryKeyValue = explictEvent.PrimaryKeyValue,
                        EntityName = string.IsNullOrEmpty(futureEvent.EntityName) ? explictEvent.EntityName : futureEvent.EntityName,
                        EventName = futureEvent.Event,
                        Payload = string.IsNullOrEmpty(explictEvent.FutureEventCtx.FutureEventPayload) ? explictEvent.Payload : explictEvent.FutureEventCtx.FutureEventPayload,
                        Metadata = explictEvent.Metadata,
                        PublisherName = explictEvent.PublisherName,
                        PublisherId = explictEvent.PublisherId,
                        DueDate = dueDate,
                    };
                    return await CreateEventImpl(explictEventToCreate).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogError("Not generating the event, Due date = {DueDate} turned out be in past for the event = {event} with future event configuration = {futureEvent} for primaryKeyValue = {primaryKeyValue}", dueDate, LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEvent), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                }
            }
            else
            {
                _logger.LogError("Due date could not calculated for the event = {event} with future event configuration = {futureEvent} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEvent), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
            }

            return null;
        }

        /// <summary>
        /// Generate Future Events.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <param name="futureEventConfig">Future Event Config.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task GenerateFutureEvents(ExplicitEvent explictEvent, EventConfig futureEventConfig)
        {
            if (explictEvent is null)
            {
                throw new ArgumentNullException(nameof(explictEvent));
            }

            if (futureEventConfig != null && futureEventConfig.GenerateEventsList != null && futureEventConfig.GenerateEventsList.Count > 0)
            {
                _logger.LogDebug("Found the following explicit event = {event} and configuration =  {futureEventConfig} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEventConfig), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                foreach (FutureEvent futureEvent in futureEventConfig.GenerateEventsList)
                {
                    _logger.LogDebug("Found the following explicit event = {event} and specific future event configuration = {futureEventConfig} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEvent), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                    await GenerateFutureEvent(explictEvent, futureEvent).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Cancel Future Event.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <param name="cancelEvent">Cancel Event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CancelFutureEvent(ExplicitEvent explictEvent, CancelEvent cancelEvent)
        {
            if (explictEvent is null)
            {
                throw new ArgumentNullException(nameof(explictEvent));
            }

            if (cancelEvent is null)
            {
                throw new ArgumentNullException(nameof(cancelEvent));
            }

            List<T> eventsToCancel = await _eventSqlServerRepository.QueryEvents(cancelEvent.Event, explictEvent.PrimaryKeyValue).ConfigureAwait(false);
            _logger.LogDebug("Found the following events to cancel for primaryKeyValue = {0}", LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
            if (eventsToCancel != null && eventsToCancel.Count > 0)
            {
                try
                {
                    await _eventSqlServerRepository.UpdateEventStatus(eventsToCancel, false).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "CancelFutureEvent generated the exception = {0}", LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                    throw;
                }
            }
            else
            {
                _logger.LogDebug("Found No events to cancel for primaryKeyValue = {0}", LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
            }
        }

        /// <summary>
        /// Cancel Future Events.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <param name="futureEventConfig">Future EventConfig.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CancelFutureEvents(ExplicitEvent explictEvent, EventConfig futureEventConfig)
        {
            if (explictEvent is null)
            {
                throw new ArgumentNullException(nameof(explictEvent));
            }

            if (futureEventConfig != null && futureEventConfig.CancelEventsList != null && futureEventConfig.CancelEventsList.Count > 0)
            {
                _logger.LogDebug("Found the following explicit event = {event} and configuration = {futureConfig} for primaryKeyValue = {workorderId}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEventConfig), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                foreach (CancelEvent cancelEvent in futureEventConfig.CancelEventsList)
                {
                    _logger.LogDebug("Found the following explicit event = {event} and specific cancel event configuration {cancelConfig} for primaryKeyValue = {workorderId}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(cancelEvent), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                    await CancelFutureEvent(explictEvent, cancelEvent).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogDebug("No events to cancel for event = {event} with primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
            }
        }

        /// <summary>
        /// Cancel Future Events.
        /// </summary>
        /// <param name="primaryKeyValue">Primary Key Value.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CancelFutureEvents(string primaryKeyValue)
        {
            List<T> eventsToCancel = await _eventSqlServerRepository.QueryEvents(primaryKeyValue).ConfigureAwait(false);
            if (eventsToCancel != null && eventsToCancel.Count > 0)
            {
                _logger.LogDebug("Found {0} the following events to cancel for primaryKeyValue = {1}", LoggerHelper.SanitizeValue(eventsToCancel.Count), LoggerHelper.SanitizeValue(primaryKeyValue));
                try
                {
                    await _eventSqlServerRepository.UpdateEventStatus(eventsToCancel, false).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "CancelFutureEvents generated the exception for primaryKeyValue =", primaryKeyValue);
                    throw;
                }
            }
            else
            {
                _logger.LogDebug("No events found to cancel for primaryKeyValue = {0}", LoggerHelper.SanitizeValue(primaryKeyValue));
            }
        }

        /// <summary>
        /// Add Metadata to the Event.
        /// </summary>
        /// <param name="key">Metadata Key.</param>
        /// <param name="value">Metadata Value.</param>
        public void AddMetadata(string key, object value)
        {
            if (!Metadata.ContainsKey(key))
            {
                Metadata.Add(key, value);
            }
        }

        private EventTrackingEntry CreateEventTrackingEntry(EntityEntry<IAuditable> auditableEntityEntry)
        {
            EventTrackingEntry eventTrackingEntry = new EventTrackingEntry
            {
                TableName = auditableEntityEntry.Metadata.GetTableName(),
                EventType = auditableEntityEntry.State.ToString(),
                TrackingId = GetTrackingId(),
                UserContext = GetUserContext(),
                Metadata = Metadata,
                Schema = auditableEntityEntry.Metadata.GetSchema()
            };
            if (_serviceConfig != null && !string.IsNullOrEmpty(_serviceConfig.PublisherName))
            {
                eventTrackingEntry.PublisherName = _serviceConfig.PublisherName;
            }

            foreach (PropertyEntry property in auditableEntityEntry.Properties)
            {
                if (property.IsTemporary)
                {
                    // value will be generated by the database, get the value after saving
                    eventTrackingEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;

                // Ignore properties flagged with the JsonIgnore Attribute.
                if (property.Metadata.PropertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }
                else if (property.Metadata.PropertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }

                if (property.Metadata.IsPrimaryKey())
                {
                    eventTrackingEntry.KeyValues.Add(propertyName, property.CurrentValue);
                    eventTrackingEntry.Payload.Add(propertyName, property.CurrentValue);
                    continue;
                }

                // Ignore version Field
                if (property.Metadata.IsConcurrencyToken)
                {
                    continue;
                }

                switch (auditableEntityEntry.State)
                {
                    case EntityState.Added:
                        eventTrackingEntry.NewValues.Add(propertyName, property.CurrentValue);
                        eventTrackingEntry.Payload.Add(propertyName, property.CurrentValue);
                        break;

                    case EntityState.Deleted:
                        eventTrackingEntry.OldValues.Add(propertyName, property.OriginalValue);
                        eventTrackingEntry.Payload.Add(propertyName, property.OriginalValue);
                        break;

                    case EntityState.Modified:
                        // let the config decide if we want to publush the current state or not.
                        if (_implicitEventsConfig == null || _implicitEventsConfig.PublishCurrentState == false)
                        {
                            if (property.IsModified)
                            {
                                // publish modified values only
                                eventTrackingEntry.OldValues.Add(propertyName, property.OriginalValue);
                                eventTrackingEntry.NewValues.Add(propertyName, property.CurrentValue);
                                eventTrackingEntry.Payload.Add(propertyName, property.CurrentValue);
                            }
                        }
                        else
                        {
                            if (property.IsModified)
                            {
                                eventTrackingEntry.OldValues.Add(propertyName, property.OriginalValue);
                                eventTrackingEntry.NewValues.Add(propertyName, property.CurrentValue);
                            }

                            eventTrackingEntry.Payload.Add(propertyName, property.CurrentValue);
                            // publish current state.
                        }

                        break;
                }
            }

            // Soft Delete, Set the Event type to Deleted since is set to Modified
            if (!auditableEntityEntry.Entity.IsActive)
            {
                eventTrackingEntry.EventType = EntityState.Deleted.ToString();
            }

            return eventTrackingEntry;
        }

        /// <summary>
        /// Add Events to PlatformDbContext for persistence.
        /// </summary>
        /// <param name="eventTrackingEntryList">List of Events.</param>
        /// <param name="platformDbContext"><see cref="PlatformDbContext"/>.</param>
        private void EvaluateEventTrackingList(List<EventTrackingEntry> eventTrackingEntryList, PlatformDbContext platformDbContext)
        {
            string currentUser = GetCurrentUser();
            foreach (EventTrackingEntry eventTrackingEntry in eventTrackingEntryList)
            {
                // Get the final value of the temporary properties
                foreach (PropertyEntry prop in eventTrackingEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        eventTrackingEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else
                    {
                        eventTrackingEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }

                    eventTrackingEntry.Payload[prop.Metadata.Name] = prop.CurrentValue;
                }

                if (_serviceConfig != null && !string.IsNullOrEmpty(_serviceConfig.PublisherName))
                {
                    eventTrackingEntry.PublisherName = _serviceConfig.PublisherName;
                }

                EventTrackingEntity eventTrackingEntity = eventTrackingEntry.CreateEntity<T>(_implicitEventsConfig);
                eventTrackingEntity.IsActive = true;
                eventTrackingEntity.CreatedBy = !string.IsNullOrEmpty(currentUser) ? currentUser : "Service running without user context"; // For example, consumer auth generates Mfa event, its running w/o user context; may be, we run that under system account?
                eventTrackingEntity.CreatedDate = DateTime.UtcNow;
                platformDbContext.Add(eventTrackingEntity);
            }
        }

        private string GetTrackingId()
        {
            string trackingId = _userHttpContextAccessorService.GetTrackingId();
            return trackingId;
        }

        private string GetUserContext()
        {
            // just get what we may need.
            IUserContext userContext = new UserContext() { UserId = _userHttpContextAccessorService?.GetCurrentUserId(), Username = _userHttpContextAccessorService?.GetCurrentUser(), TenantId = _userHttpContextAccessorService != null ? _userHttpContextAccessorService.GetTenantId() : default(long), TenantType = _userHttpContextAccessorService?.GetTenantType(), TrackingId = _userHttpContextAccessorService?.GetTrackingId() };
            return JsonSerializer.Serialize(userContext);
        }

        private string GetCurrentUser()
        {
            string currentUser = _userHttpContextAccessorService.GetCurrentUser();
            return currentUser;
        }

        /// <summary>
        /// Create Event Implementation.
        /// </summary>
        /// <param name="explictEvent"><see cref="ExplicitEvent"/>.</param>
        /// <returns>Created Event.</returns>
        private async Task<EventTrackingEntity> CreateEventImpl(ExplicitEvent explictEvent)
        {
            _logger.LogDebug("Creating Event with name = {name} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));

            // Add Custom Header or X-Forwarded-Host (EncryptionConstants.EncryptionRequestHeader) used as key identifier for encryption/decryption.
            AddEncryptionRequestHeaderToEvent(explictEvent);

            string currentUser = GetCurrentUser();
            // Create the Event
            T eventTrackingEntity = new T
            {
                PrimaryKeyValue = explictEvent.PrimaryKeyValue,
                EntityName = explictEvent.EntityName,
                EventName = explictEvent.EventName,
                Payload = explictEvent.Payload,
                Metadata = explictEvent.Metadata,
                PublisherName = explictEvent.PublisherName,
                PublisherId = explictEvent.PublisherId,
                TrackingId = GetTrackingId(),
                IsActive = true,
                CreatedBy = !string.IsNullOrEmpty(currentUser) ? currentUser : "Service running without user context", // For example, consumer auth generates Mfa event, its running w/o user context; may be, we run that under system account?
                CreatedDate = DateTime.UtcNow,
                UserContext = GetUserContext(),
                DueDate = explictEvent.DueDate
            };

            // Save Event
            T createdEvent = await _eventSqlServerRepository.CreateEvent(eventTrackingEntity).ConfigureAwait(false);
            return createdEvent;
        }

        private void AddEncryptionRequestHeaderToEvent(ExplicitEvent explictEvent)
        {
            if (!string.IsNullOrEmpty(explictEvent.Metadata))
            {
                Dictionary<string, object> explictEventMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    explictEventMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        explictEvent.Metadata,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
                }
                catch (JsonException)
                {
                    // explictEvent.Metadata is not coming as key value pair (dictionary) ignore it and continue
                    _logger.LogWarning($"Event Metadata for event {LoggerHelper.SanitizeValue(explictEvent.EventName)} is not in a key value pair format");
                }

                // Create a new dictionary to search keys with keys in uppercase to avoid duplicates,
                // tried to do it with JsonSerializetion but no luck.
                Dictionary<string, object> explictSearchEventMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, object> kv in explictEventMetadata)
                {
                    explictSearchEventMetadata.Add(kv.Key.ToUpperInvariant(), kv.Value);
                }

                if (!explictSearchEventMetadata.ContainsKey(EncryptionConstants.EncryptionRequestHeader.ToUpperInvariant()))
                {
                    string clientKeyIdentifier = _userHttpContextAccessorService.GetClientKeyIdentifier();
                    if (!string.IsNullOrEmpty(clientKeyIdentifier))
                    {
                        explictEventMetadata.Add(EncryptionConstants.EncryptionRequestHeader, clientKeyIdentifier);
                    }

                    explictEvent.Metadata = JsonSerializer.Serialize(
                        explictEventMetadata,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
                }
            }
            else
            {
                Dictionary<string, object> explictEventMetadata = new Dictionary<string, object>();
                string clientKeyIdentifier = _userHttpContextAccessorService.GetClientKeyIdentifier();
                if (!string.IsNullOrEmpty(clientKeyIdentifier))
                {
                    explictEventMetadata.Add(EncryptionConstants.EncryptionRequestHeader, clientKeyIdentifier);
                    explictEvent.Metadata = JsonSerializer.Serialize(
                        explictEventMetadata,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
                }
            }
        }

        /// <summary>
        /// Get Event Due Date.
        /// </summary>
        /// <param name="explictEvent">Explict Event.</param>
        /// <param name="futureEvent">Future Event.</param>
        /// <returns>DateTime.</returns>
        private DateTime? GetDueDate(ExplicitEvent explictEvent, FutureEvent futureEvent)
        {
            if (explictEvent.FutureEventCtx.FutureEventDueDataDataCtx != null)
            {
                object dueDateRef = GetPropValue(explictEvent.FutureEventCtx.FutureEventDueDataDataCtx, futureEvent.DueDateReference);

                if (dueDateRef != null)
                {
                    // handle both datetime and datetimeoffset?
                    DateTime dueDateTimeRefUtc = DateTime.MinValue;
                    if (dueDateRef is DateTime)
                    {
                        dueDateTimeRefUtc = (DateTime)dueDateRef;
                    }
                    else
                    {
                        if (dueDateRef is DateTimeOffset)
                        {
                            dueDateTimeRefUtc = ((DateTimeOffset)dueDateRef).UtcDateTime;
                        }
                    }

                    if (dueDateTimeRefUtc != DateTime.MinValue)
                    {
                        if (string.IsNullOrEmpty(futureEvent.DueDateExpression))
                        {
                            throw new ExosPersistenceException("Due date expression is null or empty.");
                        }

                        if (double.TryParse(futureEvent.DueDateExpression, out double toAdd))
                        {
                            switch (futureEvent.DueDateExpressionUnits)
                            {
                                case "days":
                                    dueDateTimeRefUtc = dueDateTimeRefUtc.AddDays(toAdd);
                                    break;
                                case "hours":
                                    dueDateTimeRefUtc = dueDateTimeRefUtc.AddHours(toAdd);
                                    break;
                                case "minutes":
                                    dueDateTimeRefUtc = dueDateTimeRefUtc.AddMinutes(toAdd);
                                    break;
                                case "seconds":
                                    dueDateTimeRefUtc = dueDateTimeRefUtc.AddSeconds(toAdd);
                                    break;
                                default: throw new ExosPersistenceException($"DueDateExpressionUnits is not handled for event = {explictEvent.EventName} and futureConfig = {LoggerHelper.SanitizeValue(futureEvent)} with primaryKeyValue = {explictEvent.PrimaryKeyValue}");
                            }
                        }
                        else
                        {
                            throw new ExosPersistenceException($"Due date expression cannot be parsed to valid double for event = {explictEvent.EventName} and future configuration = {LoggerHelper.SanitizeValue(futureEvent)} with primaryKeyValue = {explictEvent.PrimaryKeyValue}");
                        }

                        return dueDateTimeRefUtc;
                    }
                }
                else
                {
                    _logger.LogError("Due date could not be calculated for the event  = {event} and future configuration = {futureEvent} for primaryKeyValue =  {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEvent), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
                }
            }
            else
            {
                _logger.LogError("No Data ctx passed to calculate the due date, event = {event}, futureEvent = {futureEvent} for primaryKeyValue = {primaryKeyValue}", LoggerHelper.SanitizeValue(explictEvent.EventName), LoggerHelper.SanitizeValue(futureEvent), LoggerHelper.SanitizeValue(explictEvent.PrimaryKeyValue));
            }

            return null;
        }
    }
}
