namespace Exos.Platform.AspNetCore.Security
{
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Used to setup defaults for all <see cref="ApiKeyAuthenticationOptions" />.
    /// </summary>
    public class PostConfigureApiKeyAuthenticationOptions : IPostConfigureOptions<ApiKeyAuthenticationOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostConfigureApiKeyAuthenticationOptions"/> class.
        /// </summary>
        public PostConfigureApiKeyAuthenticationOptions()
        {
        }

        /// <summary>
        /// Invoked to post configure a TOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to configure.</param>
        public void PostConfigure(string name, ApiKeyAuthenticationOptions options)
        {
            if (options != null)
            {
                if (string.IsNullOrEmpty(options.AuthorizationScheme))
                {
                    options.AuthorizationScheme = ApiKeyAuthenticationDefaults.AuthorizationScheme;
                }

                if (string.IsNullOrEmpty(options.ApiKeyPattern))
                {
                    options.ApiKeyPattern = ApiKeyAuthenticationDefaults.ApiKeyPattern;
                }

                if (options.CacheDuration == null)
                {
                    options.CacheDuration = ApiKeyAuthenticationDefaults.CacheDuration;
                }
            }
        }
    }
}
