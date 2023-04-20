namespace Exos.Platform.BuildInformation
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Exos.Platform.BuildInformation.Helpers;

    /// <summary>
    /// The default implementation of the <see cref="IBuildInformation" /> interface.
    /// </summary>
    public class DefaultBuildInformation : IBuildInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBuildInformation" /> class using the entry assembly.
        /// </summary>
        public DefaultBuildInformation() : this(Assembly.GetEntryAssembly())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBuildInformation" /> class for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly containing build information.</param>
        public DefaultBuildInformation(Assembly assembly)
        {
            // Assembly name and version. Always available.
            AssemblyName = AssemblyHelper.GetName(assembly);
            AssemblyVersion = AssemblyHelper.GetVersion(assembly);

            // Assembly file version. May not be available in all builds.
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrEmpty(fileVersion))
            {
                AssemblyFileVersion = new Version(fileVersion);
            }

            // Build configuration. May not be available in all builds.
            BuildConfiguration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

            // Metadata attributes, if supplied by the build.
            var metadatas = assembly.GetCustomAttributes<AssemblyMetadataAttribute>() ?? Array.Empty<AssemblyMetadataAttribute>();
            foreach (AssemblyMetadataAttribute kvp in metadatas)
            {
                switch ((kvp.Key ?? string.Empty).ToUpperInvariant())
                {
                    case "BUILDTIMESTAMP":
                        if (DateTimeOffset.TryParse(kvp.Value, out var buildTimestamp))
                        {
                            BuildTimestamp = buildTimestamp.ToLocalTime();
                        }

                        break;

                    case "BUILDNUMBER":
                        BuildNumber = string.IsNullOrEmpty(kvp.Value) ? null : kvp.Value;
                        break;

                    case "SOURCEBRANCH":
                        SourceBranch = string.IsNullOrEmpty(kvp.Value) ? null : kvp.Value;
                        break;

                    case "SOURCEVERSION":
                        SourceVersion = string.IsNullOrEmpty(kvp.Value) ? null : kvp.Value;
                        break;

                    case "SOURCEPATH":
                        SourcePath = string.IsNullOrEmpty(kvp.Value) ? null : kvp.Value;
                        break;
                }
            }
        }

        /// <inheritdoc />
        public string AssemblyName { get; set; }

        /// <inheritdoc />
        public Version AssemblyVersion { get; set; }

        /// <inheritdoc />
        public Version AssemblyFileVersion { get; set; }

        /// <inheritdoc />
        public string BuildNumber { get; set; }

        /// <inheritdoc />
        public DateTimeOffset BuildTimestamp { get; set; }

        /// <inheritdoc />
        public string BuildConfiguration { get; set; }

        /// <inheritdoc />
        public string SourceBranch { get; set; }

        /// <inheritdoc />
        public string SourceVersion { get; set; }

        /// <inheritdoc />
        public string SourcePath { get; set; }
    }
}
