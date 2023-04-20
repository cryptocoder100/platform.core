#pragma warning disable  CA1052 // Static holder types should be Static or NotInheritable
namespace Exos.Platform.AspNetCore.Logging
{
    /// <summary>
    /// Establish a pattern for defining a range of integers for each application to use when logging.
    /// </summary>
    /// <remarks>
    /// Protected constants are defined to help establish ranges for each application.  This of course is just
    /// a helper pattern and each application is free to use it or not.
    /// </remarks>
    public class PlatformEvents
    {
        // Define the application's id and base event for it

        /// <summary>
        /// Application ID.
        /// </summary>
        public const int AppId = 0;

        /// <summary>
        /// Base Event.
        /// </summary>
        public const int Base = (AppId * Range) + Offset;

        /// <summary>
        /// An object was initialized from a class.
        /// </summary>
        public const int Configuration = Base + 0;

        // Define an event range for each application and an initial offset within it

        /// <summary>
        /// Initial Offset.
        /// </summary>
        protected const int Offset = 1000;

        /// <summary>
        /// Event Range.
        /// </summary>
        protected const int Range = 10000;
    }
}
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable