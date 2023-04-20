namespace Exos.Platform.Persistence.EventPoller
{
    using System;

    /// <summary>
    /// Configuration settings for EventPollerService.
    /// </summary>
    public class EventPollerIntegrationsServiceSettings
    {
        /// <summary>
        /// Gets or sets the name of the API user.
        /// </summary>
        public string ApiUserName { get; set; }

        /// <summary>
        /// Gets or sets the API token.
        /// </summary>
        public string ApiToken { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the service.
        /// </summary>
        public string Service { get; set; }
    }
}
