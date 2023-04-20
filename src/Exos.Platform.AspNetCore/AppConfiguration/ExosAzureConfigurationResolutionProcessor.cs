namespace Exos.Platform.AspNetCore.AppConfiguration
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Authentication;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This processor performs actions to resolve configuration tokens.
    /// </summary>
    public static class ExosAzureConfigurationResolutionProcessor
    {
        /// <summary>
        /// Processes the configuration by replacing "${...}" placeholders.
        /// </summary>
        /// <param name="configuration">The configuration to process.</param>
        public static void ProcessTokenResolution(IConfiguration configuration)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var resolver = new ExosAzureConfigurationResolver(configuration);
            var errorBuilder = resolver.ProcessTokens();

            if (errorBuilder.Length > 0)
            {
                throw new InvalidOperationException(errorBuilder.ToString());
            }
        }

        /// <summary>
        /// Processes the token resolution.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        [Obsolete(".NET Core configuration is synchronous. Use the synchronous version of ProcessTokenResolution.")]
        public static Task ProcessTokenResolution(IConfiguration configuration, CancellationToken cancellationToken)
        {
            ProcessTokenResolution(configuration);
            return Task.CompletedTask;
        }
    }
}
