#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// Model describing an parameter validation error.
    /// </summary>
    public class ErrorParamModel
    {
        /// <summary>
        /// Gets or sets the name of the parameter that failed validation.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the messages explaining the failed validation.
        /// </summary>
        public string[] Errors { get; set; }

        // /// <summary>
        // /// Gets or sets the root exceptions and stack traces that generated the validation error.
        // /// </summary>
        // public Exception[] Exceptions { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays