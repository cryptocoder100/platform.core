namespace Exos.Platform.Persistence.EventPoller
{
    using System;

    /// <summary>
    /// Configuration settings for EventPollerService.
    /// </summary>
    public class EventPollerServiceSettings
    {
        /// <summary>
        /// Gets or sets how long to delay after the polling loop logic completes..
        /// </summary>
        public TimeSpan EventPollingInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets query to load events.
        /// </summary>
        public string EventQuery { get; set; }

        /// <summary>
        /// Gets or sets query to load events.
        /// </summary>
        public string EventWithOffsetQuery { get; set; }

        /// <summary>
        /// Gets or sets the DeleteEventPublishCheckPointQuery.
        /// </summary>
        public string DeleteEventPublishCheckPointQuery { get; set; }

        /// <summary>
        /// Gets or sets query to update events.
        /// </summary>
        public string UpdateQuery { get; set; }

        /// <summary>
        /// Gets or sets size for each batch of event ids to update.
        /// </summary>
        public int UpdateQueryBatchSize { get; set; } = 2000;

        /// <summary>
        /// Gets or sets a value indicating whether IsPollingEnabled.
        /// </summary>
        public bool IsPollingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsArchivalEnabled.
        /// </summary>
        public bool IsArchivalEnabled { get; set; }

        /// <summary>
        /// Gets or sets table to Archive Events.
        /// </summary>
        public string ArchivalTable { get; set; }

        /// <summary>
        /// Gets or sets schema of the Table to Archive Events.
        /// </summary>
        public string ArchivalSchema { get; set; }

        /// <summary>
        /// Gets or sets Service Bus Connection String.
        /// </summary>
        public string ServiceBusConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the QueueName.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the TopicName.
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// Gets or sets the Events Retention Query.
        /// </summary>
        public string EventsRetentionQuery { get; set; }

        /// <summary>
        /// Gets or sets the Event Queue Retention Query.
        /// </summary>
        public string EventQueueRetentionQuery { get; set; }

        /// <summary>
        /// Gets or sets or set Event Publish Check Point Retention Query.
        /// </summary>
        public string EventPublishCheckPointRetentionQuery { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// the events are send in batch to service bus.
        /// </summary>
        public bool SendEventsInBatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// the events are tracked in the ICT EventTracking table.
        /// </summary>
        public bool IsIctEventTrackingEnabled { get; set; }

        /// <summary>
        /// Gets or sets or set EventsArchiveStorageReadWriteConnectionString.
        /// </summary>
        public string EventsArchiveStorageReadWriteConnectionString { get; set; }
    }
}
