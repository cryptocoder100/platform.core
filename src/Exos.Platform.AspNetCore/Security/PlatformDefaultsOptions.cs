namespace Exos.Platform.AspNetCore.Security
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Platform Default configuration.
    /// </summary>
    public class PlatformDefaultsOptions
    {
        private List<string> _trustedCerts;

        /// <summary>
        /// Gets or sets a value indicating whether to enable the <see cref="ApiResourceAuthorizationHandler" />.
        /// </summary>
        public bool AuthorizationByConvention { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether Newtonsoft JSON compatibility is enabled.
        /// </summary>
        public bool NewtonsoftJsonCompatability { get; set; } = true;

        /// <summary>
        /// Gets or sets the thumbprints for the custom trusted cert.
        /// </summary>
        public string TrustedCertsListString { get; set; }

        /// <summary>
        /// Gets or Sets the service link tenant ids.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<long> ServiceLinkTenantIds { get; set; } = new List<long> { 1 };
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets the trusted certs.
        /// </summary>
        public List<string> TrustedCerts
        {
            get
            {
                if (_trustedCerts == null)
                {
                    ReadTrustedCerts();
                }

                return _trustedCerts;
            }
        }

        private void ReadTrustedCerts()
        {
            if (string.IsNullOrWhiteSpace(TrustedCertsListString))
            {
                return;
            }

            var replacedString = TrustedCertsListString.Replace('\'', '\"');

            var json = JArray.Parse(replacedString);
            var newList = new List<string>();
            foreach (var item in json)
            {
                newList.Add(item.ToString());
            }

            _trustedCerts = newList;
        }
    }
}
