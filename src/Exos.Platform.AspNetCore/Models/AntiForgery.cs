#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable SA1402 // FileMayOnlyContainASingleType

namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// CsrfConstants.
    /// </summary>
    public static class CsrfConstants
    {
        /// <summary>
        /// Value for the CSRF HEADER TOKEN:X-XSRF-TOKEN..
        /// </summary>
        public static readonly string CSRFHEADERTOKEN = "X-XSRF-TOKEN";
    }

    /// <summary>
    /// Routes which needs ignored.
    /// </summary>
    public class TargetRoute
    {
        /// <summary>
        /// Gets or sets the Route Name..
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the allowed HTTP methods for a proxied route. The default is to allow all methods for each route..
        /// </summary>
        public List<string> Methods { get; set; }

        /// <summary>
        /// Gets or sets the pattern for matching a proxied input URL..
        /// </summary>
        public string InputPattern { get; set; }
    }

    /// <summary>
    /// ExosAntiforgeryOptions options.
    /// </summary>
    public class ExosAntiforgeryOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether isEnabled the Anti-forgery token.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the list or Routes to ignore.
        /// </summary>
        public List<TargetRoute> IgnoreRoutes { get; set; } = new List<TargetRoute>();
    }

    /// <summary>
    /// Similar to proxy route. Route to ignore.
    /// </summary>
    public class AntiforgeryRuleModel
    {
        /// <summary>
        /// Gets or sets the Regex Pattern for Rule.
        /// </summary>
        public Regex Pattern { get; set; }

        /// <summary>
        /// Gets or sets the Route to ignore.
        /// </summary>
        public TargetRoute IgnoreRoute { get; set; }
    }
}
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore SA1402 // FileMayOnlyContainASingleType