using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Helpers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.Telemetry
{
    internal class GlobalEnricherTelemetryInitializer : BaseTelemetryInitializer
    {
        private static readonly Assembly _applicationAssembly = Assembly.GetEntryAssembly();
        private static readonly BuildInformation _applicationBuildInformation = BuildInformation.FromAssembly(_applicationAssembly);

        public GlobalEnricherTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override void Execute(ITelemetry telemetry, IDictionary<string, string> properties)
        {
            EnrichWithCloudRoleName(telemetry);
            EnrichWithApplicationBuildInformation(properties);
            EnrichWithTrackingId(properties);
        }

        private static void EnrichWithCloudRoleName(ITelemetry telemetry)
        {
            // The officially support way to identify the application
            // https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-map?tabs=net#set-or-override-cloud-role-name
            telemetry.Context.Cloud.RoleName = AssemblyHelper.EntryAssemblyName;
        }

        private static void EnrichWithApplicationBuildInformation(IDictionary<string, string> properties)
        {
            properties["Application.Version"] = AssemblyHelper.EntryAssemblyVersion.ToString(3);

            if (!string.IsNullOrEmpty(_applicationBuildInformation.BuildTimestamp))
            {
                properties["Application.BuildTimestamp"] = _applicationBuildInformation.BuildTimestamp;
            }

            if (!string.IsNullOrEmpty(_applicationBuildInformation.BuildNumber))
            {
                properties["Application.BuildNumber"] = _applicationBuildInformation.BuildNumber;
            }

            if (!string.IsNullOrEmpty(_applicationBuildInformation.SourceBranch))
            {
                properties["Application.SourceBranch"] = _applicationBuildInformation.SourceBranch;
            }

            if (!string.IsNullOrEmpty(_applicationBuildInformation.SourceCommit))
            {
                properties["Application.SourceCommit"] = _applicationBuildInformation.SourceCommit;
            }
        }

        private void EnrichWithTrackingId(IDictionary<string, string> properties)
        {
            const string trackingIdKey = "TrackingId";

            var feature = HttpContextAccessor?.HttpContext?.Features?.Get<IRequestTelemetryFeature>();
            if (feature != null)
            {
                properties[trackingIdKey] = feature.TrackingId;
                return;
            }

            // Fall back to current Activity
            var trackingId = Activity.Current?.GetTrackingId();
            if (!string.IsNullOrEmpty(trackingId))
            {
                properties[trackingIdKey] = trackingId;
            }
        }
    }
}
