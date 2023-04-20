namespace Exos.Platform.AspNetCore.IntegrationTests.Extensions
{
    /// <summary>
    /// Types of Default HttpClient Platform provides.
    /// </summary>
    internal static class HttpClientTypeConstants
    {
        /// <summary>
        /// Gets option to include authentication and TrackingId.
        /// </summary>
        public const string Context = "Context";

        /// <summary>
        /// Gets no Authentication and no TrackingId.
        /// </summary>
        public const string Naive = "Naive";
    }
}
