#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.ICTLibrary.Core;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.Persistence.EventTracking;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class ICTEventPollerAzureTableStorageService<T, TCP> : ICTEventPollerBaseService<T, TCP>, IICTEventPollerCheckPointAzureTableStorageService
        where T : EventTrackingEntity, new()
        where TCP : EventPublishCheckPointEntity, new()
    {
        private const string _keyLength = "000000000000000000000";
        private static readonly CultureInfo _cultureInfo = new CultureInfo("en-US", false);
        private static PollerProcess _pollerProcess = PollerProcess.AzureTableStorage;
        private readonly IServiceProvider _services;
        private readonly ILogger<ICTEventPollerEventHubService<T, TCP>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerAzureTableStorageService{T, TCP}"/> class.
        /// </summary>
        /// <param name="services"><see cref="IServiceProvider"/>.</param>
        /// <param name="options"><see cref="EventPollerServiceSettings"/>.</param>
        /// <param name="loggerBase"><see cref="ILogger"/>.</param>
        public ICTEventPollerAzureTableStorageService(IServiceProvider services, IOptions<EventPollerServiceSettings> options, ILogger<ICTEventPollerEventHubService<T, TCP>> loggerBase)
            : base(services, options, loggerBase)
        {
            _services = services;
            _logger = loggerBase;
        }

        /// <inheritdoc/>
        public override PollerProcess GetPollerProcess()
        {
            return _pollerProcess;
        }

        /// <inheritdoc/>
        public override List<KeyValuePair<string, string>> GetAdditionalMetaData()
        {
            var lst = new List<KeyValuePair<string, string>>();
            return lst;
        }

        /// <summary>
        /// Send events to integrations event bridge.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events to send.</param>
        /// <exception cref="ArgumentNullException">eventTrackingEvents.</exception>
        public override async Task<List<T>> SendICTEvents(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents is null)
            {
                throw new ArgumentNullException(nameof(eventTrackingEvents));
            }

            List<T> failedEvents = new List<T>();
            using (var scope = _services.CreateScope())
            {
                var eventTrackingAzureEntities = eventTrackingEvents.Select(e =>
                {
                    var azureTableEventEntity = ReflectionHelper.Map<T, EventTrackingAzureTableEntity>(e);
                    azureTableEventEntity.PartitionKey = !string.IsNullOrEmpty(e.PrimaryKeyValue) ? e.PrimaryKeyValue : Guid.NewGuid().ToString();
                    azureTableEventEntity.RowKey = e.EventId.ToString(_keyLength, _cultureInfo);
                    return azureTableEventEntity;
                }).ToList();

                var eventGrps = eventTrackingAzureEntities?.GroupBy(t => t.PartitionKey)?.ToList();

                foreach (var eventGrp in eventGrps)
                {
                    try
                    {
                        var azureTableRepo = scope.ServiceProvider.GetRequiredService<ITableClientOperationsService<EventTrackingAzureTableEntity>>();
                        await azureTableRepo.ExecuteBatchAsync(eventTrackingAzureEntities.ToList()).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"ICTEventPollerAzureTableStorageService Event Poller Service Error Sending to Azure table storage Message: PartitionKey =  {eventGrp.Key} ", e.Message);
                    }
                }

                if (failedEvents.Any())
                {
                    // Remove failed messages from original list, we need to archive only succesful messages
                    eventTrackingEvents.RemoveAll(i => failedEvents.Contains(i));
                }
            }

            return failedEvents;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types