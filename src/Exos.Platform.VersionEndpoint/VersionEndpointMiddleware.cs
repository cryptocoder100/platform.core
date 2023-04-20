using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Exos.Platform.BuildInformation;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.VersionEndpoint
{
    /// <summary>
    /// Middleware for displaying application version information.
    /// </summary>
    public class VersionEndpointMiddleware
    {
        private readonly IBuildInformation _buildInformation;
        private readonly List<AssemblyName> _referencedAssemblies;
        private readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionEndpointMiddleware" /> class.
        /// </summary>
        /// <param name="buildInformation">An <see cref="IBuildInformation" /> instance.</param>
        public VersionEndpointMiddleware(IBuildInformation buildInformation)
        {
            _buildInformation = buildInformation ?? throw new ArgumentNullException(nameof(buildInformation));

            // Get referenced assemblies
            var assembly = Assembly.GetEntryAssembly();
            _referencedAssemblies = (assembly.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>()).OrderBy(a => a.Name).ToList();

            _serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        /// <summary>
        /// A function that can process an HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext" /> for the request.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Disable any caching
            context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            context.Response.Headers.Add("Pragma", "no-cache");
            context.Response.Headers.Add("Expires", "0");

            // JSON mime type
            context.Response.Headers.Add("Content-Type", "application/json");

            var versionInfo = new
            {
                AssemblyName = _buildInformation.AssemblyName,
                AssemblyVersion = _buildInformation.AssemblyVersion.ToString(),
                AssemblyFileVersion = _buildInformation.AssemblyFileVersion.ToString(),
                BuildNumber = _buildInformation.BuildNumber,
                BuildTimestamp = _buildInformation.BuildTimestamp,
                BuildConfiguration = _buildInformation.BuildConfiguration,
                SourceBranch = _buildInformation.SourceBranch,
                SourceVersion = _buildInformation.SourceVersion,
                SourcePath = _buildInformation.SourcePath,
                ReferencedAssemblies = _referencedAssemblies.Select(a =>
                {
                    return new
                    {
                        AssemblyName = a.Name,
                        AssemblyVersion = a.Version.ToString()
                    };
                })
            };

            // Display version info (human readable)
            var json = JsonSerializer.Serialize(versionInfo, _serializerOptions);
            return context.Response.WriteAsync(json, context.RequestAborted);
        }
    }
}
