#pragma warning disable CA1034 // Nested types should not be visible
namespace Exos.Platform.ICTLibrary.Core
{
    /// <summary>
    /// ICT Configuration Section.
    /// </summary>
    public static class IctConfigSection
    {
        /// <summary>
        /// ICT Message Section.
        /// </summary>
        public class MessageSection
        {
            /// <summary>
            /// Gets or Sets the ICt database connection.
            /// </summary>
            public string IctDb { get; set; }
        }
    }
}
#pragma warning restore CA1034 // Nested types should not be visible
