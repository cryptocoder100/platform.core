namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension methods for the HttpContext class.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Deserializes a JSON stream to the object type specified.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="content">Current HttpContent.</param>
        /// <param name="settings">The <see cref="JsonSerializerSettings" /> to use for deserialization.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content, JsonSerializerSettings settings = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var serializer = settings == null ? new JsonSerializer() : JsonSerializer.Create(settings);
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
