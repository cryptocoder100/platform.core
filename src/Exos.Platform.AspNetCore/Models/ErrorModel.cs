#pragma warning disable CA1819 // Properties should not return arrays

namespace Exos.Platform.AspNetCore.Models
{
    using System;

    /// <summary>
    /// Model describing an error response.
    /// </summary>
    public class ErrorModel
    {
        /// <summary>
        /// Gets the model type. This always returns <see cref="ModelType.Error" />.
        /// </summary>
        public ModelType Model { get; } = ModelType.Error;

        /// <summary>
        /// Gets or sets the <see cref="ErrorType" /> enumeration value.
        /// </summary>
        public ErrorType Type { get; set; }

        /// <summary>
        /// Gets or sets the time the error occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the tracking ID associated with this request.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly message for the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the list of parameters that failed validation.
        /// </summary>
        public ErrorParamModel[] Params { get; set; }

        /// <summary>
        /// Gets or sets the root exception and stack trace that generated the error.
        /// </summary>
        public string Exception { get; set; }
    }
}
#pragma warning restore CA1819 // Properties should not return arrays