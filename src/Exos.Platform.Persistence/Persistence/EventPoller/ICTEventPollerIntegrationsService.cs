#pragma warning disable CA1715 // Identifiers should have correct prefix
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.ICTLibrary.Core;
    using Exos.Platform.Persistence.EventTracking;
    using Exos.Platform.Persistence.Persistence.EventPoller;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class ICTEventPollerIntegrationsService<T, TCP> : ICTEventPollerBaseService<T, TCP>, IICTEventPollerCheckPointIntegrationsService
        where T : EventTrackingEntity, new()
        where TCP : EventPublishCheckPointEntity, new()
    {
        private static PollerProcess _pollerProcess = PollerProcess.Integrations;
        private readonly ILogger<ICTEventPollerIntegrationsService<T, TCP>> _logger;
        private readonly IServiceProvider _services;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<EventPollerIntegrationsServiceSettings> _eventBridgeOptions;
        private readonly IAppTokenProvider _appTokenProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerIntegrationsService{T, TCP}" /> class.
        /// </summary>
        /// <param name="services">
        ///   <see cref="IServiceProvider" />.</param>
        /// <param name="options">
        ///   <see cref="EventPollerServiceSettings" />.</param>
        /// <param name="eventBridgeOptions">The event bridge options.</param>
        /// <param name="logger">ICTEventPollerServiceBusService <see cref="ILogger" />.</param>
        /// <param name="loggerBase">ICTEventPollerEventHubService <see cref="ILogger" />.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="appTokenProvider">The application token provider.</param>
        public ICTEventPollerIntegrationsService(
            IServiceProvider services,
            IOptions<EventPollerServiceSettings> options,
            IOptions<EventPollerIntegrationsServiceSettings> eventBridgeOptions,
            ILogger<ICTEventPollerIntegrationsService<T, TCP>> logger,
            ILogger<ICTEventPollerEventHubService<T, TCP>> loggerBase,
            IHttpClientFactory httpClientFactory,
            IAppTokenProvider appTokenProvider) : base(services, options, loggerBase)
        {
            _logger = logger;
            _services = services;
            _httpClientFactory = httpClientFactory;
            _eventBridgeOptions = eventBridgeOptions;
            _appTokenProvider = appTokenProvider;
        }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        protected HttpClient HttpClient => _httpClientFactory.CreateClient("Native");

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
                var ictEventPublisher = scope.ServiceProvider.GetRequiredService<IIctEventPublisher>();
                int numberOfMessages = 0;
                int failedMessages = 0;
                foreach (T eventTrackingEntity in eventTrackingEvents)
                {
                    try
                    {
                        numberOfMessages++;

                        IntegrationsEventQueueModel requestModel = ReflectionHelper.Map<T, IntegrationsEventQueueModel>(eventTrackingEntity);
                        requestModel.SourceService = _eventBridgeOptions.Value.Service;
                        string serializedObject = JsonSerializer.Serialize(requestModel);

                        await PostDataAsync(serializedObject);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"ICT Scheduler Event Poller Service Error Sending to ICT Message: {numberOfMessages} - Event Id =  {eventTrackingEntity.EventId} ", ex.Message);
                        failedMessages++;
                        failedEvents.Add(eventTrackingEntity);
                        return failedEvents;

                        // break out & return on first failure,we will check point & let process retry from there.
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

        /// <summary>
        /// Adds the authentication to header.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ArgumentNullException">request.</exception>
        private async Task AddAuthToHeader(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string apiUserName = _eventBridgeOptions.Value.ApiUserName;
            var apiPassword = _eventBridgeOptions.Value.ApiToken;

            var accessToken = await _appTokenProvider.GetToken(apiUserName, apiPassword).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        /// <summary>
        /// Posts the data asynchronous.
        /// </summary>
        /// <param name="json">The json.</param>
        private async Task PostDataAsync(string json)
        {
            var requestUri = new Uri(_eventBridgeOptions.Value.Endpoint);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            requestMessage.Content = content;
            await AddAuthToHeader(requestMessage);

            using HttpResponseMessage response = await HttpClient.SendAsync(requestMessage).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                // log verified no PII.
                _logger.LogError(ex, "PostDataAsync to {requestUri} failed at {responseCode}", requestUri.AbsolutePath, response.StatusCode);
                throw;
            }
        }
    }
}
#pragma warning restore CA1715 // Identifiers should have correct prefix
#pragma warning restore CA1031 // Do not catch general exception types
