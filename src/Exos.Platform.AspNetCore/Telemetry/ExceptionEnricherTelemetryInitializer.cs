#pragma warning disable CA1308 // Normalize strings to uppercase

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Exos.Platform.AspNetCore.Helpers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;

namespace Exos.Platform.Telemetry
{
    internal sealed class ExceptionEnricherTelemetryInitializer : BaseTelemetryInitializer
    {
        private const double _kilobyte = 1024;
        private const double _megabyte = _kilobyte * _kilobyte;

        private static readonly ObjectPool<StringBuilder> _stringBuilderPool = new DefaultObjectPoolProvider().Create(new StringBuilderPooledObjectPolicy() { InitialCapacity = 256 });

        private static readonly BuildInformation _applicationBuildInformation = BuildInformation.FromAssembly(AssemblyHelper.EntryAssembly);
        private static readonly BuildInformation _platformBuildInformation = BuildInformation.FromAssembly(AssemblyHelper.PlatformAssembly);

        public ExceptionEnricherTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override void Execute(ITelemetry telemetry, IDictionary<string, string> properties)
        {
            if (!(telemetry is ExceptionTelemetry exceptionTelemetry))
            {
                // Not exception telemetry
                return;
            }

            EnrichWithAdditionalApplicationBuildInformation(properties);
            EnrichWithPlatformBuildInformation(properties);
            EnrichWithSystemInformation(properties);
            EnrichWithProcessInformation(properties);
            EnrichWithRequestInformation(properties);
        }

        private static double BytesToMegabytes(double bytes) => bytes / _megabyte;

        private static void EnrichWithAdditionalApplicationBuildInformation(IDictionary<string, string> properties)
        {
            if (!string.IsNullOrEmpty(_applicationBuildInformation.BuildConfiguration))
            {
                properties["Application.BuildConfiguration"] = _applicationBuildInformation.BuildConfiguration;
            }

            if (!string.IsNullOrEmpty(_applicationBuildInformation.SourcePath))
            {
                properties["Application.SourcePath"] = _applicationBuildInformation.SourcePath;
            }
        }

        private static void EnrichWithPlatformBuildInformation(IDictionary<string, string> properties)
        {
            properties["Platform.Version"] = AssemblyHelper.GetVersion(AssemblyHelper.PlatformAssembly).ToString(3);

            if (!string.IsNullOrEmpty(_platformBuildInformation.BuildConfiguration))
            {
                properties["Platform.BuildConfiguration"] = _platformBuildInformation.BuildConfiguration;
            }

            if (!string.IsNullOrEmpty(_platformBuildInformation.BuildTimestamp))
            {
                properties["Platform.BuildTimestamp"] = _platformBuildInformation.BuildTimestamp;
            }

            if (!string.IsNullOrEmpty(_platformBuildInformation.BuildNumber))
            {
                properties["Platform.BuildNumber"] = _platformBuildInformation.BuildNumber;
            }

            if (!string.IsNullOrEmpty(_platformBuildInformation.SourceBranch))
            {
                properties["Platform.SourceBranch"] = _platformBuildInformation.SourceBranch;
            }

            if (!string.IsNullOrEmpty(_platformBuildInformation.SourceCommit))
            {
                properties["Platform.SourceCommit"] = _platformBuildInformation.SourceCommit;
            }

            if (!string.IsNullOrEmpty(_platformBuildInformation.SourcePath))
            {
                properties["Platform.SourcePath"] = _platformBuildInformation.SourcePath;
            }
        }

        private static void EnrichWithSystemInformation(IDictionary<string, string> properties)
        {
            properties["System.OS"] = RuntimeInformation.OSDescription;
            properties["System.Runtime"] = RuntimeInformation.FrameworkDescription;
            properties["System.Cores"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);
            properties["System.Clock"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        private static void EnrichWithProcessInformation(IDictionary<string, string> properties)
        {
            using var process = Process.GetCurrentProcess();
            var id = process.Id;

            ThreadPool.GetMinThreads(out int minWrkrs, out int minComplWrkers);
            ThreadPool.GetMaxThreads(out int maxWrkrs, out int maxComplWrkers);

            properties["Process.ID"] = process.Id.ToString(CultureInfo.InvariantCulture);
            properties["Process.IOThreads"] = $"{minComplWrkers} <-> {maxComplWrkers}";
            properties["Process.WorkerThreads"] = $"{minWrkrs} <-> {maxWrkrs}";
            properties["Process.WorkingSet"] = BytesToMegabytes(Environment.WorkingSet).ToString("N0", CultureInfo.InvariantCulture) + "MB";
            properties["Process.ThreadId"] = Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture);
        }

        private void EnrichWithRequestInformation(IDictionary<string, string> properties)
        {
            var feature = HttpContextAccessor?.HttpContext?.Features?.Get<IRequestTelemetryFeature>();
            if (feature == null)
            {
                return;
            }

            /*
            if (!string.IsNullOrEmpty(feature.RedactedReferrerUrl))
            {
                properties["Url"] = feature.RedactedUrl;
            }

            if (!string.IsNullOrEmpty(feature.RedactedReferrerUrl))
            {
                properties["Referrer"] = feature.RedactedReferrerUrl;
            }

            if (!string.IsNullOrEmpty(feature.RedactedAuthorizationHeader))
            {
                properties["AuthorizationHeader"] = feature.RedactedAuthorizationHeader;
            }
            */

            if (feature.RedactedHeaders != null && feature.RedactedHeaders.Count > 0)
            {
                var sb = _stringBuilderPool.Get();

                try
                {
                    // Build an HTTP-style headers string
                    foreach (var header in feature.RedactedHeaders)
                    {
#pragma warning disable CA1305 // Specify IFormatProvider
                        sb.AppendLine($"{header.Key}: {header.Value}");
#pragma warning restore CA1305 // Specify IFormatProvider
                    }

                    properties["Request.Headers"] = sb.ToString();
                }
                finally
                {
                    _stringBuilderPool.Return(sb);
                }
            }
        }
    }
}
