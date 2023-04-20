namespace Exos.Platform.BuildInformation
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Provides access to metadata provided by the build process.
    /// </summary>
    public interface IBuildInformation
    {
        /// <summary>
        /// Gets or sets the assembly name.
        /// </summary>
        string AssemblyName { get; set; }

        /// <summary>
        /// Gets or sets the assembly version.
        /// </summary>
        Version AssemblyVersion { get; set; }

        /// <summary>
        /// Gets or sets the assembly file version.
        /// </summary>
        Version AssemblyFileVersion { get; set; }

        /// <summary>
        /// Gets or sets the build number, if supplied by the build process.
        /// </summary>
        string BuildNumber { get; set; }

        /// <summary>
        /// Gets or sets the time the build was run, if supplied by the build process.
        /// </summary>
        DateTimeOffset BuildTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the build configuration used, if supplied by the build process.
        /// </summary>
        string BuildConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the Git branch used in the build, if supplied by the build process.
        /// </summary>
        string SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the Git commit used in the build, if supplied by the build process.
        /// </summary>
        string SourceVersion { get; set; }

        /// <summary>
        /// Gets or sets the Azure DevOps project->repository path.
        /// </summary>
        string SourcePath { get; set; }
    }
}
