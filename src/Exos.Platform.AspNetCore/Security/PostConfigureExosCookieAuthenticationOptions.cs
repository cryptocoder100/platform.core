namespace Exos.Platform.AspNetCore.Security
{
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Used to setup defaults for all <see cref="ExosCookieAuthenticationOptions" />.
    /// </summary>
    public class PostConfigureExosCookieAuthenticationOptions : IPostConfigureOptions<ExosCookieAuthenticationOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostConfigureExosCookieAuthenticationOptions"/> class.
        /// </summary>
        public PostConfigureExosCookieAuthenticationOptions()
        {
        }

        /// <summary>
        /// Invoked to post configure a TOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to configure.</param>
        public void PostConfigure(string name, ExosCookieAuthenticationOptions options)
        {
            if (options != null)
            {
                if (string.IsNullOrEmpty(options.Name))
                {
                    // $/FieldServices/SpaLogin/Dev4_EXOS_RC1/src/UI/SpaLogin/Web.config
                    options.Name = ".myauth";
                }
            }
        }
    }
}
