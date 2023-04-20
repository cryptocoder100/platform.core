#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA2234 // Pass system uri objects instead of strings

namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Exos.Platform.AspNetCore.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Implementation of ITelemetryProcessor.
    /// Mask sensitive values in the telemetry entries
    /// Values to mask are configured in the MaskTelemetryValues section of the appsettings.
    /// </summary>
    public class MaskTelemetryProcessor : ITelemetryProcessor
    {
        private const string _redactedReplacement = "REDACTED";

        private readonly ITelemetryProcessor _next;
        private readonly MaskTelemetryProcessorOptions _maskTelemetryProcessorOptions;
        private readonly IList<string> _keywords;
        private readonly string _maskValues;
        private readonly string _regularExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskTelemetryProcessor"/> class.
        /// </summary>
        /// <param name="next">The next <see cref="ITelemetryProcessor" />.</param>
        /// <param name="maskTelemetryProcessorOptions">The maskTelemetryProcessorOptions<see cref="IOptions{MaskTelemetryProcessorOptions}"/>.</param>
        public MaskTelemetryProcessor(ITelemetryProcessor next, IOptions<MaskTelemetryProcessorOptions> maskTelemetryProcessorOptions)
        {
            _next = next;
            _maskTelemetryProcessorOptions = maskTelemetryProcessorOptions?.Value;
            _keywords = new List<string>();

            if (maskTelemetryProcessorOptions != null && _maskTelemetryProcessorOptions.MaskTelemetryValues.Any())
            {
                _keywords = _maskTelemetryProcessorOptions.MaskTelemetryValues;
                _maskValues = string.Join("|", _maskTelemetryProcessorOptions.MaskTelemetryValues);
                _regularExpression = $"w*({_maskValues})([^\\s]+)";
            }
        }

        /// <inheritdoc/>
        public void Process(ITelemetry item)
        {
            try
            {
                ProcessUnsafe(item);
            }
            catch (Exception ex)
            {
                // Don't bubble-up; logging must never fail
                Debug.WriteLine(ex);

                try
                {
                    var supportProperties = item as ISupportProperties;
                    if (supportProperties != null)
                    {
                        supportProperties.Properties["TelemetryError"] = ex.ToString();
                    }
                }
                catch (Exception ex2)
                {
                    // Don't bubble-up; logging must never fail
                    Debug.WriteLine(ex2);
                }
            }
            finally
            {
                _next.Process(item);
            }
        }

        private void ProcessUnsafe(ITelemetry item)
        {
            if (item is TraceTelemetry traceTelemetry)
            {
                if (!string.IsNullOrEmpty(traceTelemetry.Message))
                {
                    var properties = traceTelemetry.Properties;
                    var keys = new List<string>(properties.Keys);

                    foreach (var key in keys)
                    {
                        var value = properties[key];
                        properties[key] = GetMaskedValue(value);
                    }

                    traceTelemetry.Message = GetMaskedValue(traceTelemetry.Message);
                }
            }
            else if (item is ExceptionTelemetry exceptionTelemetry)
            {
                /*
                // NOTE: Temporary workaround for JWT related exceptions until we get better masking logic
                if (!string.IsNullOrEmpty(exceptionTelemetry.Message) && !(exceptionTelemetry.Exception is SecurityTokenExpiredException) && !(exceptionTelemetry.Exception is UnauthorizedException))
                {
                    var properties = exceptionTelemetry.Properties;
                    var keys = new List<string>(properties.Keys);

                    foreach (var key in keys)
                    {
                        var value = properties[key];
                        properties[key] = GetMaskedValue(value);
                    }

                    // Masking content of the exception
                    foreach (var exceptionDetailsInfo in exceptionTelemetry.ExceptionDetailsInfoList)
                    {
                        exceptionDetailsInfo.Message = GetMaskedValue(exceptionDetailsInfo.Message);
                    }
                }
                */
            }
            else if (item is DependencyTelemetry dependencyTelemetry)
            {
                var properties = dependencyTelemetry.Properties;

                if (string.Equals(dependencyTelemetry.Type, "HTTP", StringComparison.OrdinalIgnoreCase))
                {
                    // Mask query strings in the dependency URL
                    dependencyTelemetry.Data = TelemetryHelper.RedactUrl(dependencyTelemetry.Data, _keywords, _redactedReplacement);
                }
                else if (string.Equals(dependencyTelemetry.Type, "SQLQuery", StringComparison.OrdinalIgnoreCase))
                {
                    // N/A because parameterized SQL will not have any values being logged
                }
                else if (string.Equals(dependencyTelemetry.Type, "COSMOSDB", StringComparison.OrdinalIgnoreCase))
                {
                    if (properties.TryGetValue("sqlQuerySpec", out var sqlQuerySpec))
                    {
                        // Mask param values in Cosmos query spec
                        // properties["sqlQuerySpec"] = TelemetryHelper.RedactSqlQuerySpec(sqlQuerySpec, _keywords, _redactedReplacement);
                    }
                }
            }
            else if (item is RequestTelemetry requestTelemetry)
            {
                // Mask query strings in the request
                requestTelemetry.Url = TelemetryHelper.RedactUrl(requestTelemetry.Url, _keywords, _redactedReplacement);

                if (requestTelemetry.Properties.ContainsKey("Referrer"))
                {
                    // Mask query strings in the referrer property
                    requestTelemetry.Properties["Referrer"] = TelemetryHelper.RedactUrl(requestTelemetry.Properties["Referrer"], _keywords, _redactedReplacement);
                }
            }
        }

        private string GetMaskedValue(string valueToMask)
        {
            if (string.IsNullOrEmpty(valueToMask))
            {
                return valueToMask;
            }

            string maskedValue = Regex.Replace(
                valueToMask,
                _regularExpression,
                matchEvaluator =>
                {
                    Group group = matchEvaluator.Groups[2];
                    StringBuilder matchValue = new StringBuilder(matchEvaluator.Value);
                    matchValue.Replace(group.Value, _redactedReplacement, group.Index - matchEvaluator.Index, group.Length);
                    return matchValue.ToString();
                },
                RegexOptions.IgnoreCase);
            return maskedValue;
        }
    }
}
