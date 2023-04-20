namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Used to setup defaults for all <see cref="ExosAuthTokenAuthenticationOptions" />.
    /// </summary>
    public class PostConfigureExosAuthTokenAuthenticationOptions : IPostConfigureOptions<ExosAuthTokenAuthenticationOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostConfigureExosAuthTokenAuthenticationOptions"/> class.
        /// </summary>
        public PostConfigureExosAuthTokenAuthenticationOptions()
        {
        }

        /// <summary>
        /// Invoked to post configure a TOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to configure.</param>
        public void PostConfigure(string name, ExosAuthTokenAuthenticationOptions options)
        {
        }
    }
}
