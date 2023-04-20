#pragma warning disable CA1715 // Identifiers should have correct prefix
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using Exos.Platform.ICTLibrary.Core;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.Persistence.EventTracking;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class ICTEventPollerEventHubService<T, TCP> : ICTEventPollerBaseService<T, TCP>, IICTEventPollerCheckPointEventHubService
        where T : EventTrackingEntity, new()
        where TCP : EventPublishCheckPointEntity, new()
    {
        private static PollerProcess _pollerProcess = PollerProcess.EventHub;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerEventHubService{T, TCP}"/> class.
        /// </summary>
        /// <param name="services"><see cref="IServiceProvider"/>.</param>
        /// <param name="options"><see cref="EventPollerServiceSettings"/>.</param>
        /// <param name="loggerBase"><see cref="ILogger"/>.</param>
        public ICTEventPollerEventHubService(IServiceProvider services, IOptions<EventPollerServiceSettings> options, ILogger<ICTEventPollerEventHubService<T, TCP>> loggerBase)
            : base(services, options, loggerBase)
        {
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
            lst.Add(new KeyValuePair<string, string>(Constants.TargetMessagingPlatform, TargetMessagingPlatform.EventHub.ToString()));
            return lst;
        }
    }
}
#pragma warning restore CA1715 // Identifiers should have correct prefix
