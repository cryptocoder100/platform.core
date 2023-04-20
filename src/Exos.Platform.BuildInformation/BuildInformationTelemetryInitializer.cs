namespace Exos.Platform.BuildInformation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Enriches Application Insights telemetry with build information from <see cref="IBuildInformation" />.
    /// </summary>
    public class BuildInformationTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IBuildInformation _buildInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildInformationTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="buildInformation">An <see cref="IBuildInformation" /> instance.</param>
        public BuildInformationTelemetryInitializer(IBuildInformation buildInformation)
        {
            _buildInformation = buildInformation; // Nulls allowed
        }

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null || _buildInformation == null)
            {
                // Nothing to do
                return;
            }

            var supportProperties = telemetry as ISupportProperties;
            if (supportProperties == null)
            {
                // Wrong kind of telemetry
                return;
            }

            // NOTE: Key names match those in core GlobalEnricherTelemetryInitializer.
            //       These only exist for services not already calling AddExosPlatformDefaults.

            // Guaranteed
            // supportProperties.Properties[nameof(IBuildInformation.AssemblyName)] = _buildInformation.AssemblyName;
            supportProperties.Properties["Application.Version"] = _buildInformation.AssemblyVersion.ToString(3);

            // Optional
            if (!string.IsNullOrEmpty(_buildInformation.BuildNumber))
            {
                supportProperties.Properties["Application.BuildNumber"] = _buildInformation.BuildNumber;
            }

            if (_buildInformation.BuildTimestamp != default)
            {
                supportProperties.Properties["Application.BuildTimestamp"] = _buildInformation.BuildTimestamp.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(_buildInformation.BuildConfiguration))
            {
                supportProperties.Properties["Application.BuildConfiguration"] = _buildInformation.BuildConfiguration;
            }

            if (!string.IsNullOrEmpty(_buildInformation.SourceBranch))
            {
                supportProperties.Properties["Application.SourceBranch"] = _buildInformation.SourceBranch;
            }

            if (!string.IsNullOrEmpty(_buildInformation.SourceVersion))
            {
                supportProperties.Properties["Application.SourceCommit"] = _buildInformation.SourceVersion;
            }
        }
    }
}
