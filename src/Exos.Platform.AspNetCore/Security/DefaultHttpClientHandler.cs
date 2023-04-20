namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// Custom Exos default HttpClientHandler that should be used by all HttpClients.
    /// <inheritdoc />
    public class DefaultHttpClientHandler : HttpClientHandler
    {
        private readonly PlatformDefaultsOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHttpClientHandler"/> class.
        /// </summary>
        /// <param name="options">The PlatformDefaultsOptions.</param>
        public DefaultHttpClientHandler(PlatformDefaultsOptions options)
        {
            _options = options;

            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            ServerCertificateCustomValidationCallback += OnServerCertValidation;

            // Fiddler support
            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!string.IsNullOrEmpty(httpProxy))
            {
                Proxy = new WebProxy(httpProxy);
            }
        }

        private static byte[] GetRawData(string certPem)
        {
            var stripped = certPem.Replace("-----BEGIN CERTIFICATE-----", string.Empty, StringComparison.OrdinalIgnoreCase);
            stripped = stripped.Replace("-----END CERTIFICATE-----", string.Empty, StringComparison.OrdinalIgnoreCase);

            return Convert.FromBase64String(stripped);
        }

        private bool OnServerCertValidation(HttpRequestMessage request, X509Certificate2 cert, X509Chain certificateChain, SslPolicyErrors sslErrors)
        {
            if (sslErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (certificateChain.ChainStatus.Any(status => status.Status != X509ChainStatusFlags.UntrustedRoot))
            {
                return false;
            }

            if (_options.TrustedCerts.Any() == false)
            {
                return false;
            }

            var trustedCertsData = _options.TrustedCerts.Select(GetRawData).ToArray();

            foreach (var element in certificateChain.ChainElements)
            {
                foreach (var status in element.ChainElementStatus)
                {
                    if (status.Status == X509ChainStatusFlags.UntrustedRoot)
                    {
                        // Check that the root certificate matches one of the valid root certificates
                        if (trustedCertsData.Any(c => c.SequenceEqual(element.Certificate.RawData)))
                        {
                            continue; // Process the next status
                        }
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
