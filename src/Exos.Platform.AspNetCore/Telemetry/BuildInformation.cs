using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Exos.Platform.AspNetCore.Helpers;

namespace Exos.Platform.Telemetry
{
    // NOTE: This is effectively duplicate effort to what we have in Exos.Platform.BuildInformation
    // copied here so that we can access it in this package.
    internal sealed class BuildInformation
    {
        public string BuildConfiguration { get; set; }

        public string BuildTimestamp { get; set; }

        public string BuildNumber { get; set; }

        public string SourceBranch { get; set; }

        public string SourceCommit { get; set; }

        public string SourcePath { get; set; }

        public static BuildInformation FromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>() ?? Enumerable.Empty<AssemblyMetadataAttribute>();
            return new BuildInformation
            {
                BuildConfiguration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                BuildTimestamp = metadata.FirstOrDefault(am => "BUILDTIMESTAMP".Equals(am.Key, StringComparison.OrdinalIgnoreCase))?.Value,
                BuildNumber = metadata.FirstOrDefault(am => "BUILDNUMBER".Equals(am.Key, StringComparison.OrdinalIgnoreCase))?.Value,
                SourceBranch = metadata.FirstOrDefault(am => "SOURCEBRANCH".Equals(am.Key, StringComparison.OrdinalIgnoreCase))?.Value,
                SourceCommit = metadata.FirstOrDefault(am => "SOURCEVERSION".Equals(am.Key, StringComparison.OrdinalIgnoreCase))?.Value,
                SourcePath = metadata.FirstOrDefault(am => "SOURCEPATH".Equals(am.Key, StringComparison.OrdinalIgnoreCase))?.Value
            };
        }
    }
}
