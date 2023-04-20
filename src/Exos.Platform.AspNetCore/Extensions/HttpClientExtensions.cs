using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Exos.Platform.AspNetCore.Extensions
{
    /// <summary>
    /// HttpClient Extensions.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// This extension method for <see cref="HttpClient"/> provides a convenient overload that accepts headers with content body.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance.</param>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The path to the requested target.</param>
        /// <param name="headers">Key Vaule Pair Headers.</param>
        /// <param name="requestBody">requestBody.</param>
        /// <param name="cancellationToken">The body of the request.</param>
        /// <returns>HttpResponseMessage.</returns>
        public static Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, HttpMethod method, Uri requestUri, Dictionary<string, string> headers = null, object requestBody = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            using var request = GetHttpRequestMessage(method, requestUri, requestBody, headers);
            return httpClient.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// This extension method for <see cref="HttpClient"/> provides a convenient overload that accepts a Bearer accessToken to be used as Bearer authentication.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance.</param>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The path to the requested target.</param>
        /// <param name="accessToken">The access token to be used as Bearer authentication.</param>
        /// <param name="headers">Key Vaule Pair Headers.</param>
        /// <param name="requestBody">The body of the request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>HttpResponseMessage.</returns>
        public static async Task<HttpResponseMessage> SendRequestWithBearerTokenAsync(this HttpClient httpClient, HttpMethod method, Uri requestUri, string accessToken, Dictionary<string, string> headers = null, object requestBody = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            using var request = GetHttpRequestMessage(method, requestUri, requestBody, headers);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.SendAsync(request, cancellationToken);
            return response;
        }

        /// <summary>
        /// Get HttpRequestMessage.
        /// </summary>
        /// <typeparam name="T">Generic Object.</typeparam>
        /// <param name="method">method.</param>
        /// <param name="requestUri">requestUri.</param>
        /// <param name="requestBody">requestBody.</param>
        /// <param name="headers">headers.</param>
        /// <returns>HttpRequestMessage.</returns>
        private static HttpRequestMessage GetHttpRequestMessage<T>(HttpMethod method, Uri requestUri, T requestBody, Dictionary<string, string> headers)
        {
            var request = new HttpRequestMessage(method, requestUri);

            if (requestBody != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            }

            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.Add(key, value);
                }
            }

            return request;
        }
    }
}
