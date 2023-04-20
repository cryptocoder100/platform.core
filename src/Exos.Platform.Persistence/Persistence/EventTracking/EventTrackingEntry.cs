#pragma warning disable CA2227 //Collection properties should be read only
namespace Exos.Platform.Persistence.EventTracking
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Exos.Platform.Persistence.Models;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    /// <summary>
    /// Create a Event Tracking Entry.
    /// </summary>
    public class EventTrackingEntry
    {
        /// <summary>
        /// Gets or sets the EventType.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the TableName.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the TrackingId.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the UserContext.
        /// </summary>
        public string UserContext { get; set; }

        /// <summary>
        /// Gets the KeyValues.
        /// </summary>
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the OldValues.
        /// </summary>
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the NewValues.
        /// </summary>
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the Payload.
        /// </summary>
        public Dictionary<string, object> Payload { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the Metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the TemporaryProperties.
        /// </summary>
        public List<PropertyEntry> TemporaryProperties { get; } = new List<PropertyEntry>();

        /// <summary>
        /// Gets a value indicating whether HasTemporaryProperties has any item.
        /// </summary>
        public bool HasTemporaryProperties => TemporaryProperties.Any();

        /// <summary>
        /// Gets or sets PublisherName.
        /// </summary>
        public string PublisherName { get; set; }

        /// <summary>
        /// Gets or sets Schema.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Create a tracking Entity.
        /// </summary>
        /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
        /// <param name="implicitEventsConfig"><see cref="ImplicitEventsConfig"/>.</param>
        /// <returns>Returns <see cref="EventTrackingEntity"/>.</returns>
        public EventTrackingEntity CreateEntity<T>(ImplicitEventsConfig implicitEventsConfig) where T : EventTrackingEntity, new()
        {
            // Create this settings to not apply camel case to column names
            var dictionarySerializationSettings = new JsonSerializerOptions
            { PropertyNameCaseInsensitive = false };

            EventTrackingEntity eventTrackingEntity = new T
            {
                EventName = $"{TableName}.{EventType}",
                EntityName = TableName,
                TrackingId = TrackingId,
                UserContext = UserContext,
                PrimaryKeyValue = (implicitEventsConfig != null && implicitEventsConfig.TryGetSingleKeyValue && KeyValues != null && KeyValues.Count == 1 && !KeyValues.FirstOrDefault().Equals(default(KeyValuePair<string, object>))) ? KeyValues.FirstOrDefault().Value.ToString() : JsonSerializer.Serialize(KeyValues, dictionarySerializationSettings),
                PublisherName = PublisherName
            };

            if (implicitEventsConfig == null || implicitEventsConfig.PublishConsolidatedPayload == false)
            {
                eventTrackingEntity.OldValue = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues, dictionarySerializationSettings);
                eventTrackingEntity.NewValue = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues, dictionarySerializationSettings);
            }
            else
            {
                // publishing old & new values with the payLoad.
                if (OldValues.Count != 0)
                {
                    Payload.Add("OldValues_EventTracking", OldValues);
                }

                if (NewValues.Count != 0)
                {
                    Payload.Add("NewValues_EventTracking", NewValues);
                }
            }

            eventTrackingEntity.Payload = Payload.Count == 0 ? null : JsonSerializer.Serialize(Payload, dictionarySerializationSettings);

            StringBuilder primaryKeyValue = new StringBuilder();
            foreach (var keyValue in KeyValues)
            {
                primaryKeyValue.Append(keyValue.Key + "_" + keyValue.Value + "_");
            }

            if (primaryKeyValue.Length > 0)
            {
                primaryKeyValue.Length--;
            }

            // meta data specific to the entity.
            var localMetaData = new Dictionary<string, object>
            {
                { "PartitionKey", PublisherName + "_" + Schema + "_" + eventTrackingEntity.EntityName + "_" + primaryKeyValue.ToString() },
                { "Schema", Schema }
            };

            // meta data provided by the caller.
            if (Metadata != null && Metadata.Count > 0)
            {
                foreach (var keyVal in Metadata)
                {
                    localMetaData.Add(keyVal.Key, keyVal.Value);
                }
            }

            eventTrackingEntity.Metadata = localMetaData.Count == 0 ? null : JsonSerializer.Serialize(localMetaData, dictionarySerializationSettings);
            return eventTrackingEntity;
        }
    }
}
#pragma warning restore CA2227 //Collection properties should be read only